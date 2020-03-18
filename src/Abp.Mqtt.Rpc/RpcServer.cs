using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Abp.Mqtt.Rpc.Internal;
using Abp.Mqtt.Serialization;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;

namespace Abp.Mqtt.Rpc
{
    public class RpcServer : IRpcServer, IDisposable
    {
        private readonly RpcAwareApplicationMessageReceivedHandler _applicationMessageReceivedHandler;
        private readonly CancellationTokenSource _exitSource = new CancellationTokenSource();
        private readonly Dictionary<string, MethodInfo> _methods;
        private readonly IManagedMqttClient _mqttClient;
        private readonly ConcurrentDictionary<CancellationTokenSource, Task> _noIdCalls = new ConcurrentDictionary<CancellationTokenSource, Task>();
        private readonly IMessageSerializer _serializer;
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<string, TaskInfo> _waitingCalls = new ConcurrentDictionary<string, TaskInfo>();

        public RpcServer(IManagedMqttClient mqttClient, IMessageSerializer serializer, IServiceProvider serviceProvider)
        {
            _mqttClient = mqttClient;
            _serializer = serializer;
            _serviceProvider = serviceProvider;
            _methods = serviceProvider.GetServices<IRpcService>().SelectMany(s => s.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance))
                .ToDictionary(mi => mi.Name, mi => mi);
            _applicationMessageReceivedHandler = new RpcAwareApplicationMessageReceivedHandler(
                _mqttClient.ApplicationMessageReceivedHandler,
                HandleApplicationMessageReceivedAsync);

            Start();
        }

        public string ServerId => _mqttClient.Options.ClientOptions.ClientId;

        public bool Started { get; private set; }

        public void Dispose()
        {
            ForceStop();
            _exitSource.Dispose();
        }

        public void Start()
        {
            if (Started) return;
            Started = true;
            _mqttClient.ApplicationMessageReceivedHandler = _applicationMessageReceivedHandler;
            InitSubscription().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public void Wait()
        {
            _exitSource.Token.WaitHandle.WaitOne();
        }

        public async Task Stop(TimeSpan timeout = default)
        {
            var start = DateTime.Now;

            if (timeout == default) timeout = Timeout.InfiniteTimeSpan;

            _mqttClient.ApplicationMessageReceivedHandler = _applicationMessageReceivedHandler.OriginalHandler;

            while (timeout == Timeout.InfiniteTimeSpan || DateTime.Now - start > timeout)
            {
                if (_waitingCalls.IsEmpty && _noIdCalls.IsEmpty)
                {
                    Started = false;
                    _exitSource.Cancel();
                    return;
                }

                await Task.Delay(100); // TODO
            }

            ForceStop();
        }

        public void ForceStop()
        {
            _mqttClient.ApplicationMessageReceivedHandler = _applicationMessageReceivedHandler.OriginalHandler;

            foreach (var taskInfo in _waitingCalls.Values) taskInfo.CancellationTokenSource.Cancel();
            foreach (var cancellationTokenSource in _noIdCalls.Keys) cancellationTokenSource.Cancel();

            _waitingCalls.Clear();
            _noIdCalls.Clear();

            Started = false;
            _exitSource.Cancel();
        }

        public async Task InitSubscription()
        {
            // await _mqttClient.SubscribeAsync(GetRequestTopic(), MqttQualityOfServiceLevel.ExactlyOnce);
            await _mqttClient.SubscribeAsync(GetRequestTopic("+"), MqttQualityOfServiceLevel.ExactlyOnce);
        }

        private string GetResponseTopic(string source = null)
        {
            return $"from/{ServerId}/rpc_response" + (string.IsNullOrEmpty(source) ? "" : $"/to/{source}");
        }

        private string GetRequestTopic(string source = null)
        {
            return (string.IsNullOrEmpty(source) ? "" : $"from/{source}/") + $"rpc/to/{ServerId}";
        }

        private string GetSource(string requestTopic)
        {
            if (!requestTopic.StartsWith("from/", StringComparison.OrdinalIgnoreCase)) return null;
            var endIndex = requestTopic.IndexOf('/', 5);
            return requestTopic.Substring(5, endIndex - 5);
        }

        private async Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            var message = eventArgs.ApplicationMessage;
            var methodName = message.UserProperties.First(up => up.Name.Equals("Method", StringComparison.OrdinalIgnoreCase)).Value;
            var id = message.UserProperties.FirstOrDefault(up => up.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))?.Value;
            var timeout = message.UserProperties.FirstOrDefault(up => up.Name.Equals("Timeout", StringComparison.OrdinalIgnoreCase))?.Value;

            if (!_methods.TryGetValue(methodName, out var method))
            {
                if (!string.IsNullOrEmpty(id))
                {
                    var response = new MqttApplicationMessageBuilder()
                        .WithTopic(GetResponseTopic(GetSource(message.Topic)))
                        .WithUserProperty("Id", id)
                        .WithUserProperty("Success", false.ToString())
                        .WithUserProperty("Message", $"找不到方法 '{methodName}' 。")
                        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                        .Build();
                    await _mqttClient.PublishAsync(response).ConfigureAwait(false);
                }

                return;
            }

            var parameterInfo = method.GetParameters().SingleOrDefault(); // TODO: More than one

            object[] args;
            if (parameterInfo == null)
                args = null;
            else if (parameterInfo.ParameterType == typeof(byte[]))
                args = new[] {(object) message.Payload};
            else
                args = new[] {_serializer.Deserialize(message.Payload, parameterInfo.ParameterType)};

            using var serviceScope = _serviceProvider.CreateScope();
            using var cts = string.IsNullOrEmpty(timeout) ? new CancellationTokenSource() : new CancellationTokenSource(int.Parse(timeout));
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
                    if (!_waitingCalls.TryAdd(id, new TaskInfo(task, cts))) throw new InvalidOperationException();
                }
                else
                {
                    _noIdCalls.TryAdd(cts, task);
                }

                var result = await task;

                if (string.IsNullOrEmpty(id)) return;

                var response = new MqttApplicationMessageBuilder()
                    .WithTopic(GetResponseTopic(GetSource(message.Topic)))
                    .WithUserProperty("Id", id)
                    .WithUserProperty("Success", true.ToString())
                    .WithContentType(_serializer.ContentType)
                    .WithPayload(_serializer.Serialize(result))
                    .WithAtLeastOnceQoS()
                    .Build();
                await _mqttClient.PublishAsync(response).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (string.IsNullOrEmpty(id)) return;
                var response = new MqttApplicationMessageBuilder()
                    .WithTopic(GetResponseTopic(GetSource(message.Topic)))
                    .WithUserProperty("Id", id)
                    .WithUserProperty("Success", false.ToString())
                    .WithUserProperty("Message", ex.Message)
                    .WithAtLeastOnceQoS()
                    .Build();
                await _mqttClient.PublishAsync(response).ConfigureAwait(false);
            }
            finally
            {
                if (!string.IsNullOrEmpty(id))
                    _waitingCalls.TryRemove(id, out _);
                else
                    _noIdCalls.TryRemove(cts, out _);
            }
        }
    }
}
