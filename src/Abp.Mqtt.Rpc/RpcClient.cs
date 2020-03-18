using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Abp.Mqtt.Serialization;
using MQTTnet;
using MQTTnet.Exceptions;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;

namespace Abp.Mqtt.Rpc
{
    public class RpcClient : IDisposable
    {
        private readonly RpcAwareApplicationMessageReceivedHandler _applicationMessageReceivedHandler;
        private readonly IManagedMqttClient _mqttClient;
        private readonly ImmutableSortedDictionary<string, IMessageSerializer> _serializers;

        private readonly ConcurrentDictionary<string, TaskCompletionSource<MqttApplicationMessage>> _waitingCalls =
            new ConcurrentDictionary<string, TaskCompletionSource<MqttApplicationMessage>>();

        public RpcClient(IManagedMqttClient mqttClient, MqttConfigurator configurator)
        {
            _mqttClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));
            _serializers = configurator.MessageSerializers.ToImmutableSortedDictionary(serializer => serializer.ContentType, serializer => serializer);

            _applicationMessageReceivedHandler = new RpcAwareApplicationMessageReceivedHandler(
                _mqttClient.ApplicationMessageReceivedHandler,
                HandleApplicationMessageReceivedAsync);

            _mqttClient.ApplicationMessageReceivedHandler = _applicationMessageReceivedHandler;

            InitSubscription().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private IMessageSerializer DefaultSerializer => _serializers.First().Value;

        public string ClientId => _mqttClient.Options.ClientOptions.ClientId;

        public void Dispose()
        {
            _mqttClient.ApplicationMessageReceivedHandler = _applicationMessageReceivedHandler.OriginalHandler;

            foreach (var tcs in _waitingCalls.Values) tcs.TrySetCanceled();

            _waitingCalls.Clear();
        }

        private async Task InitSubscription()
        {
            // await _mqttClient.SubscribeAsync(GetResponseTopic(), MqttQualityOfServiceLevel.AtLeastOnce);
            await _mqttClient.SubscribeAsync(GetResponseTopic("+"), MqttQualityOfServiceLevel.AtLeastOnce);
        }

        public Task<T> ExecuteAsync<T>(string methodName, object payload, MqttQualityOfServiceLevel qos, TimeSpan timeout,
            string target = null, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<T>(methodName, DefaultSerializer.Serialize(payload), DefaultSerializer.ContentType, qos, timeout, target, cancellationToken);
        }

        public async Task<T> ExecuteAsync<T>(string methodName, byte[] payload, string contentType, MqttQualityOfServiceLevel qos, TimeSpan timeout,
            string target = null, CancellationToken cancellationToken = default)
        {
            var response = await ExecuteAsync(methodName, payload, contentType, qos, timeout, target, cancellationToken);
            return Deserialize<T>(response.ContentType, response.Payload);
        }

        public async Task<MqttApplicationMessage> ExecuteAsync(string methodName, byte[] payload, string contentType, MqttQualityOfServiceLevel qos, TimeSpan timeout,
            string target = null, CancellationToken cancellationToken = default)
        {
            var id = Guid.NewGuid().ToString("N");

            var requestMessage = BuildMessage(methodName, payload, contentType, qos, timeout, target, builder => { builder.WithUserProperty("Id", id); });

            try
            {
                var tcs = new TaskCompletionSource<MqttApplicationMessage>();
                if (!_waitingCalls.TryAdd(id, tcs)) throw new InvalidOperationException();

                await _mqttClient.PublishAsync(requestMessage).ConfigureAwait(false);

                using var timeoutCts = new CancellationTokenSource(timeout);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                linkedCts.Token.Register(() =>
                {
                    if (!tcs.Task.IsCompleted && !tcs.Task.IsFaulted && !tcs.Task.IsCanceled) tcs.TrySetCanceled();
                });

                try
                {
                    var result = await tcs.Task.ConfigureAwait(false);
                    timeoutCts.Cancel(false);
                    return result;
                }
                catch (OperationCanceledException exception)
                {
                    if (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                    {
                        throw new MqttCommunicationTimedOutException(exception);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            finally
            {
                _waitingCalls.TryRemove(id, out _);
            }
        }

        public Task FireAndForgetAsync(string target, string methodName, object args, MqttQualityOfServiceLevel qos,
            TimeSpan timeout = default)
        {
            return FireAndForgetAsync(target, methodName, DefaultSerializer.Serialize(args), DefaultSerializer.ContentType, qos, timeout);
        }

        public async Task FireAndForgetAsync(string target, string methodName, byte[] payload, string contentType, MqttQualityOfServiceLevel qos,
            TimeSpan timeout = default)
        {
            var requestMessage = BuildMessage(methodName, payload, contentType, qos, timeout, target);
            await _mqttClient.PublishAsync(requestMessage).ConfigureAwait(false);
        }

        private string GetRequestTopic(string target = null)
        {
            return $"from/{ClientId}/rpc" + (string.IsNullOrEmpty(target) ? "" : $"/to/{target}");
        }

        private string GetResponseTopic(string target = null)
        {
            return (string.IsNullOrEmpty(target) ? "" : $"from/{target}/") + $"rpc_response/to/{ClientId}";
        }

        private MqttApplicationMessage BuildMessage(string methodName, byte[] payload, string contentType, MqttQualityOfServiceLevel qos, TimeSpan timeout, string target,
            Action<MqttApplicationMessageBuilder> action = null)
        {
            if (methodName == null) throw new ArgumentNullException(nameof(methodName));

            if (methodName.Contains("/") || methodName.Contains("+") || methodName.Contains("#")) throw new ArgumentException("The method name cannot contain /, + or #.");

            if (!(_mqttClient.ApplicationMessageReceivedHandler is RpcAwareApplicationMessageReceivedHandler))
            {
                throw new InvalidOperationException("The application message received handler was modified.");
            }

            var builder = new MqttApplicationMessageBuilder()
                .WithTopic(GetRequestTopic(target))
                .WithPayloadFormatIndicator(MqttPayloadFormatIndicator.Unspecified)
                .WithContentType(contentType)
                .WithUserProperty("Method", methodName)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(qos);

            if (timeout != default && timeout != Timeout.InfiniteTimeSpan)
            {
                builder
                    .WithMessageExpiryInterval(Convert.ToUInt32(timeout.TotalSeconds))
                    .WithUserProperty("Timeout", Convert.ToInt32(timeout.TotalMilliseconds).ToString());
            }

            action?.Invoke(builder);

            return builder.Build();
        }

        private Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            var idProperty = eventArgs.ApplicationMessage.UserProperties
                .FirstOrDefault(up => up.Name.Equals("id", StringComparison.OrdinalIgnoreCase));

            if (idProperty != null && _waitingCalls.TryRemove(idProperty.Value, out var tcs))
            {
                var message = eventArgs.ApplicationMessage;
                var success = bool.Parse(message.GetUserProperty("Success"));
                if (success)
                {
                    tcs.TrySetResult(eventArgs.ApplicationMessage);
                }
                else
                {
                    var exception = message.GetUserProperty("Message");
                    tcs.TrySetException(new RpcException(exception));
                }
            }

            return Task.CompletedTask;
        }

        private T Deserialize<T>(string contentType, byte[] payload)
        {
            if (!_serializers.TryGetValue(contentType, out var serializer))
            {
                throw new ArgumentOutOfRangeException(nameof(contentType), $"Deserialize error: Invalid content type '{contentType}'.");
            }

            return serializer.Deserialize<T>(payload);
        }
    }
}
