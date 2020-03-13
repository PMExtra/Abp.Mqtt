using System;
using System.Collections.Concurrent;
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
        private readonly IMessageSerializer _serializer;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<byte[]>> _waitingCalls = new ConcurrentDictionary<string, TaskCompletionSource<byte[]>>();

        public RpcClient(IManagedMqttClient mqttClient, IMessageSerializer serializer)
        {
            _mqttClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));
            _serializer = serializer;

            _applicationMessageReceivedHandler = new RpcAwareApplicationMessageReceivedHandler(
                _mqttClient.ApplicationMessageReceivedHandler,
                HandleApplicationMessageReceivedAsync);

            _mqttClient.ApplicationMessageReceivedHandler = _applicationMessageReceivedHandler;

            InitSubscription().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public string ClientId => _mqttClient.Options.ClientOptions.ClientId;

        public void Dispose()
        {
            _mqttClient.ApplicationMessageReceivedHandler = _applicationMessageReceivedHandler.OriginalHandler;

            foreach (var tcs in _waitingCalls.Values)
            {
                tcs.TrySetCanceled();
            }

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
            return ExecuteAsync<T>(methodName, _serializer.Serialize(payload), _serializer.ContentType, qos, timeout, target, cancellationToken);
        }

        public async Task<T> ExecuteAsync<T>(string methodName, byte[] payload, string contentType, MqttQualityOfServiceLevel qos, TimeSpan timeout,
            string target = null, CancellationToken cancellationToken = default)
        {
            var response = await ExecuteAsync(methodName, payload, contentType, qos, timeout, target, cancellationToken);
            return _serializer.Deserialize<T>(response);
        }

        public async Task<byte[]> ExecuteAsync(string methodName, byte[] payload, string contentType, MqttQualityOfServiceLevel qos, TimeSpan timeout,
            string target = null, CancellationToken cancellationToken = default)
        {
            var id = Guid.NewGuid().ToString("N");

            var requestMessage = BuildMessage(methodName, payload, contentType, qos, timeout, target, builder =>
            {
                builder.WithUserProperty("Id", id);
            });

            try
            {
                var tcs = new TaskCompletionSource<byte[]>();
                if (!_waitingCalls.TryAdd(id, tcs))
                {
                    throw new InvalidOperationException();
                }

                await _mqttClient.PublishAsync(requestMessage).ConfigureAwait(false);

                using var timeoutCts = new CancellationTokenSource(timeout);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                linkedCts.Token.Register(() =>
                {
                    if (!tcs.Task.IsCompleted && !tcs.Task.IsFaulted && !tcs.Task.IsCanceled)
                    {
                        tcs.TrySetCanceled();
                    }
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

        public Task FireAndForgetAsync(string methodName, object args, MqttQualityOfServiceLevel qos,
            TimeSpan timeout = default, string target = null)
        {
            return FireAndForgetAsync(methodName, _serializer.Serialize(args), _serializer.ContentType, qos, timeout, target);
        }

        public async Task FireAndForgetAsync(string methodName, byte[] payload, string contentType, MqttQualityOfServiceLevel qos,
            TimeSpan timeout = default, string target = null)
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

            if (methodName.Contains("/") || methodName.Contains("+") || methodName.Contains("#"))
            {
                throw new ArgumentException("The method name cannot contain /, + or #.");
            }

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
                .WithQualityOfServiceLevel(qos)
                .WithMessageExpiryInterval(Convert.ToUInt32(timeout.TotalSeconds));

            if (timeout != default && timeout != Timeout.InfiniteTimeSpan)
            {
                builder.WithUserProperty("Timeout", Convert.ToInt32(timeout.TotalMilliseconds).ToString());
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
                    tcs.TrySetResult(eventArgs.ApplicationMessage.Payload);
                }
                else
                {
                    var exception = message.GetUserProperty("Message");
                    tcs.TrySetException(new RpcException(exception));
                }
            }

            return Task.CompletedTask;
        }
    }
}
