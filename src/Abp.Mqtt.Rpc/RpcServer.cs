using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Abp.Extensions;
using Abp.Mqtt.Extensions;
using Abp.Mqtt.Rpc.Internal;
using Abp.Mqtt.Rpc.Serialization;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Client.Receiving;
using MQTTnet.Diagnostics;
using MQTTnet.Extensions;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;

namespace Abp.Mqtt.Rpc
{
    public class RpcServer : IRpcServer, IDisposable
    {
        private readonly IMqttApplicationMessageReceivedHandler _handler;
        private readonly IMqttNetChildLogger _logger;
        private readonly Dictionary<string, MethodInfo> _methods;
        private readonly IManagedMqttClient _mqttClient;
        private readonly ConcurrentDictionary<CancellationTokenSource, Task> _noIdCalls = new ConcurrentDictionary<CancellationTokenSource, Task>();
        private readonly ImmutableSortedDictionary<string, IMessageSerializer> _serializers;
        private readonly IServiceProvider _serviceProvider;
        private readonly CancellationTokenSource _stop = new CancellationTokenSource();
        private readonly ConcurrentDictionary<string, CancellationTask> _waitingCalls = new ConcurrentDictionary<string, CancellationTask>();

        public RpcServer(IRpcServerOptions options, IServiceProvider serviceProvider, IMqttNetChildLogger logger)
        {
            _mqttClient = options.MqttClient;
            _serializers = options.Serializers.ToImmutableSortedDictionary(serializer => serializer.ContentType, serializer => serializer);
            _serviceProvider = serviceProvider;
            _logger = logger.CreateChildLogger(nameof(RpcServer));
            _methods = serviceProvider.GetServices<IRpcService>()
                .SelectMany(s => s.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance))
                .ToDictionary(mi => mi.Name, mi => mi);
            _handler = new MqttApplicationMessageReceivedHandlerDelegate(HandleApplicationMessageReceivedAsync);
            Start();
        }

        public string ServerId => _mqttClient.Options.ClientOptions.ClientId;

        private IMessageSerializer DefaultSerializer => _serializers.First().Value;

        public bool Started { get; private set; }

        public void Dispose()
        {
            ForceStop();
            _stop.Dispose();
        }

        public void Start()
        {
            if (Started) return;
            Started = true;
            _mqttClient.ApplicationMessageReceivedHandler = _mqttClient.ApplicationMessageReceivedHandler.Combine(_handler);

            InitSubscription().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public void Wait()
        {
            _stop.Token.WaitHandle.WaitOne();
        }

        public async Task Stop(TimeSpan timeout = default)
        {
            if (timeout == default) timeout = Timeout.InfiniteTimeSpan;

            var calls = _waitingCalls.Values.Select(x => x.Task).Concat(_noIdCalls.Values).ToArray();

            try
            {
                _logger.Info($"Stopping {GetType().Name}, waiting {calls.Length} calls");
                await Task.Run(async () => await Task.WhenAll(calls), new CancellationTokenSource(timeout).Token);
                StopInternal();
                _logger.Info($"{GetType().Name} has stopped.");
            }
            catch (TaskCanceledException ex)
            {
                _logger.Warning(ex, $"Stop {GetType().Name} timed out, trying to force stop ...");
                ForceStop();
            }
        }

        public void ForceStop()
        {
            StopInternal();
            _logger.Info($"{GetType().Name} has forced stop.");
        }

        private void StopInternal()
        {
            if (_mqttClient.ApplicationMessageReceivedHandler == _handler)
            {
                _mqttClient.ApplicationMessageReceivedHandler = null;
            }
            // ReSharper disable once SuspiciousTypeConversion.Global
            else if (_mqttClient.ApplicationMessageProcessedHandler is CombinedMqttApplicationMessageReceivedHandler combined)
            {
                combined.TryRemove(_handler);
            }

            foreach (var taskInfo in _waitingCalls.Values) taskInfo.CancellationTokenSource.Cancel();
            foreach (var cancellationTokenSource in _noIdCalls.Keys) cancellationTokenSource.Cancel();

            _waitingCalls.Clear();
            _noIdCalls.Clear();

            Started = false;
            _stop.Cancel();
        }

        public async Task InitSubscription()
        {
            await _mqttClient.SubscribeAsync(GetRequestTopic("+", MqttQualityOfServiceLevel.AtMostOnce), MqttQualityOfServiceLevel.AtMostOnce);
            await _mqttClient.SubscribeAsync(GetRequestTopic("+", MqttQualityOfServiceLevel.AtLeastOnce), MqttQualityOfServiceLevel.AtLeastOnce);
            await _mqttClient.SubscribeAsync(GetRequestTopic("+", MqttQualityOfServiceLevel.ExactlyOnce), MqttQualityOfServiceLevel.ExactlyOnce);
        }

        private string GetResponseTopic(string target)
        {
            return $"from/{ServerId}/rpc_response/to/{target}";
        }

        private string GetRequestTopic(string source, MqttQualityOfServiceLevel? qos)
        {
            return (source.IsNullOrEmpty() ? "" : $"from/{source}") + $"/rpc/to/{ServerId}/" + (qos == null ? "" : $"qos{(int) qos.Value}");
        }

        private string GetSource(string requestTopic)
        {
            if (!requestTopic.StartsWith("from/", StringComparison.OrdinalIgnoreCase)) return null;
            var endIndex = requestTopic.IndexOf('/', 5);
            return requestTopic.Substring(5, endIndex - 5);
        }

        private async Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            if (!eventArgs.ApplicationMessage.Topic.Contains(GetRequestTopic(null, null))) return;

            var message = eventArgs.ApplicationMessage;
            var methodName = message.GetUserProperty("Method");
            var id = message.GetUserProperty("Id");
            var timeout = message.GetUserProperty<int?>("Timeout");
            var broadcast = message.GetUserProperty<bool?>("Broadcast") ?? false;
            var noResponse = message.GetUserProperty<bool?>("NoResponse") ?? false;

            var messageBuilder = new MqttApplicationMessageBuilder()
                .WithTopic(GetResponseTopic(GetSource(message.Topic)))
                .WithUserProperty("Id", id)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce);

            if (!_methods.TryGetValue(methodName, out var method))
            {
                if (string.IsNullOrEmpty(id) || noResponse) return;

                var responseBuilder = messageBuilder
                    .WithUserProperty("Success", false.ToString())
                    .WithUserProperty("ErrorCode", "404")
                    .WithUserProperty("ErrorMessage", $"Method '{methodName}' not found.");
                if (broadcast) responseBuilder.WithUserProperty("Broadcast", true.ToString());
                if (timeout > 0) responseBuilder.WithMessageExpiryInterval((uint) timeout.Value);
                await _mqttClient.PublishAsync(responseBuilder.Build()).ConfigureAwait(false);

                return;
            }

            var parameters = method.GetParameters();

            object[] args;
            switch (parameters.Length)
            {
                case 0:
                    args = null;
                    break;

                case 1:
                    var parameterInfo = parameters.First();
                    args = parameterInfo.ParameterType == typeof(byte[])
                        ? new[] {(object) message.Payload}
                        : new[] {DefaultSerializer.Deserialize(message.Payload, parameterInfo.ParameterType)};
                    break;

                default:
                    _logger.Error(new NotImplementedException(), "Multiple parameters resolving has not been supported yet, please use a key-value object.");
                    return;
            }

            using var serviceScope = _serviceProvider.CreateScope();
            using var cts = timeout.HasValue ? new CancellationTokenSource(timeout.Value * 1000) : new CancellationTokenSource();
            try
            {
                var rpcService = serviceScope.ServiceProvider.GetService(method.DeclaringType);

                var task = Task.Run(async () =>
                {
                    var returnValue = method.Invoke(rpcService, args);
                    if (returnValue is Task t)
                    {
                        await t.ConfigureAwait(false);
                        if (t.GetType().IsGenericType)
                        {
                            var resultProperty = t.GetType().GetProperty("Result");
                            Debug.Assert(resultProperty != null);
                            returnValue = resultProperty.GetValue(t);
                        }
                    }

                    return returnValue;
                }, cts.Token);

                if (!string.IsNullOrEmpty(id))
                {
                    if (!_waitingCalls.TryAdd(id, new CancellationTask(task, cts))) throw new InvalidOperationException();
                }
                else
                {
                    _noIdCalls.TryAdd(cts, task);
                }

                var result = await task.ConfigureAwait(false);

                if (noResponse) return;

                var responseBuilder = new MqttApplicationMessageBuilder()
                    .WithTopic(GetResponseTopic(GetSource(message.Topic)))
                    .WithUserProperty("Id", id)
                    .WithUserProperty("Success", true.ToString())
                    .WithContentType(DefaultSerializer.ContentType)
                    .WithPayload(DefaultSerializer.Serialize(result))
                    .WithAtLeastOnceQoS();
                if (broadcast) responseBuilder.WithUserProperty("Broadcast", true.ToString());
                if (timeout > 0) responseBuilder.WithMessageExpiryInterval((uint) timeout.Value);
                await _mqttClient.PublishAsync(responseBuilder.Build()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (noResponse) return;

                var responseBuilder = new MqttApplicationMessageBuilder()
                    .WithTopic(GetResponseTopic(GetSource(message.Topic)))
                    .WithUserProperty("Id", id)
                    .WithUserProperty("Success", false.ToString())
                    .WithUserProperty("ErrorCode", ex.HResult.ToString())
                    .WithUserProperty("ErrorMessage", ex.Message)
                    .WithAtLeastOnceQoS();
                if (broadcast) responseBuilder.WithUserProperty("Broadcast", true.ToString());
                if (timeout > 0) responseBuilder.WithMessageExpiryInterval((uint) timeout.Value);
                await _mqttClient.PublishAsync(responseBuilder.Build()).ConfigureAwait(false);
            }
            finally
            {
                if (!string.IsNullOrEmpty(id))
                {
                    _waitingCalls.TryRemove(id, out _);
                }
                else
                {
                    _noIdCalls.TryRemove(cts, out _);
                }
            }
        }
    }
}
