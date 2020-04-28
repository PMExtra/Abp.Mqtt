using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Abp.Mqtt.Contexts;
using Abp.Mqtt.Extensions;
using Abp.Mqtt.Serialization;
using MQTTnet;
using MQTTnet.Client.Receiving;
using MQTTnet.Exceptions;
using MQTTnet.Extensions;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;

namespace Abp.Mqtt.Rpc
{
    public abstract class RpcClientBase : IDisposable
    {
        private readonly ManagedMqttContext _context;
        private readonly IMqttApplicationMessageReceivedHandler _handler;

        private readonly AutoResetEvent _initialLocking = new AutoResetEvent(true);

        private readonly ConcurrentDictionary<string, TaskCompletionSource<MqttApplicationMessage>> _waitingCalls =
            new ConcurrentDictionary<string, TaskCompletionSource<MqttApplicationMessage>>();

        private bool _initialized;

        protected RpcClientBase(ManagedMqttContext context)
        {
            _context = context;
            _handler = new MqttApplicationMessageReceivedHandlerDelegate(HandleApplicationMessageReceived);
            MqttClient.ApplicationMessageReceivedHandler = MqttClient.ApplicationMessageReceivedHandler.Combine(_handler);
        }

        private IManagedMqttClient MqttClient => _context.ManagedMqttClient;

        private ImmutableSortedDictionary<string, IMessageSerializer> Serializers => _context.Serializers;

        private IMessageSerializer DefaultSerializer => Serializers.First().Value;

        protected string ClientId => MqttClient.Options.ClientOptions.ClientId;

        public void Dispose()
        {
            foreach (var tcs in _waitingCalls.Values) tcs.TrySetCanceled();

            if (MqttClient.ApplicationMessageReceivedHandler == _handler)
            {
                MqttClient.ApplicationMessageProcessedHandler = null;
            }
            else if (MqttClient.ApplicationMessageReceivedHandler is CombinedMqttApplicationMessageReceivedHandler combined)
            {
                combined.TryRemove(_handler);
            }

            _waitingCalls.Clear();
        }

        private async Task InitSubscription()
        {
            if (_initialized) return;
            _initialLocking.WaitOne();
            if (_initialized) return;
            await MqttClient.SubscribeAsync(GetResponseTopic("+"), MqttQualityOfServiceLevel.AtLeastOnce);
            _initialized = true;
            _initialLocking.Set();
        }

        protected Task ExecuteAsync(string target, string methodName, object payload, MqttQualityOfServiceLevel qos, TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(target, methodName, DefaultSerializer.Serialize(payload), DefaultSerializer.UTF8, DefaultSerializer.ContentType, qos, timeout, cancellationToken);
        }

        protected Task<T> ExecuteAsync<T>(string target, string methodName, object payload, MqttQualityOfServiceLevel qos, TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<T>(target, methodName, DefaultSerializer.Serialize(payload), DefaultSerializer.UTF8, DefaultSerializer.ContentType, qos, timeout,
                cancellationToken);
        }

        protected async Task<T> ExecuteAsync<T>(string target, string methodName, byte[] payload, bool utf8Payload, string contentType, MqttQualityOfServiceLevel qos,
            TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            var response = await ExecuteAsync(target, methodName, payload, utf8Payload, contentType, qos, timeout, cancellationToken);
            return Deserialize<T>(response.ContentType, response.Payload);
        }

        protected async Task<MqttApplicationMessage> ExecuteAsync(string target, string methodName, byte[] payload, bool utf8Payload, string contentType,
            MqttQualityOfServiceLevel qos, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            await InitSubscription();

            var id = Guid.NewGuid().ToString("N");

            var requestMessage = BuildMessage(methodName, payload, utf8Payload, contentType, qos, timeout, target, builder => { builder.WithUserProperty("Id", id); });

            try
            {
                var tcs = new TaskCompletionSource<MqttApplicationMessage>();
                if (!_waitingCalls.TryAdd(id, tcs)) throw new InvalidOperationException();

                await MqttClient.PublishAsync(requestMessage).ConfigureAwait(false);

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

        protected Task FireAndForgetAsync(string target, string methodName, object args, MqttQualityOfServiceLevel qos, TimeSpan timeout = default)
        {
            return FireAndForgetAsync(target, methodName, DefaultSerializer.Serialize(args), DefaultSerializer.UTF8, DefaultSerializer.ContentType, qos, timeout);
        }

        protected async Task FireAndForgetAsync(string target, string methodName, byte[] payload, bool utf8Payload, string contentType, MqttQualityOfServiceLevel qos,
            TimeSpan timeout = default)
        {
            var requestMessage = BuildMessage(methodName, payload, utf8Payload, contentType, qos, timeout, target,
                builder => builder.WithUserProperty("NoResponse", true.ToString()));
            await MqttClient.PublishAsync(requestMessage).ConfigureAwait(false);
        }

        private string GetRequestTopic(string target, MqttQualityOfServiceLevel qos)
        {
            var topic = $"from/{ClientId}/rpc";
            if (!string.IsNullOrEmpty(target)) topic += $"/to/{target}";
            topic += $"/qos{(int) qos}";
            return topic;
        }

        private string GetResponseTopic(string source)
        {
            return (string.IsNullOrEmpty(source) ? "" : $"from/{source}") + $"/rpc_response/to/{ClientId}";
        }

        private MqttApplicationMessage BuildMessage(string methodName, byte[] payload, bool utf8Payload, string contentType, MqttQualityOfServiceLevel qos, TimeSpan timeout,
            string target, Action<MqttApplicationMessageBuilder> action = null)
        {
            if (methodName == null) throw new ArgumentNullException(nameof(methodName));

            var builder = new MqttApplicationMessageBuilder()
                .WithTopic(GetRequestTopic(target, qos))
                .WithContentType(contentType)
                .WithUserProperty("Method", methodName)
                .WithQualityOfServiceLevel(qos);

            if (payload != null)
            {
                builder
                    .WithPayload(payload)
                    .WithPayloadFormatIndicator(utf8Payload ? MqttPayloadFormatIndicator.CharacterData : MqttPayloadFormatIndicator.Unspecified);
            }

            if (timeout != default && timeout != Timeout.InfiniteTimeSpan)
            {
                builder
                    .WithMessageExpiryInterval(Convert.ToUInt32(timeout.TotalSeconds))
                    .WithUserProperty("Timeout", Convert.ToInt32(timeout.TotalSeconds).ToString());
            }

            action?.Invoke(builder);

            return builder.Build();
        }

        private void HandleApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            if (!eventArgs.ApplicationMessage.Topic.Contains(GetResponseTopic(null)))
            {
                return;
            }

            var idProperty = eventArgs.ApplicationMessage.UserProperties
                .FirstOrDefault(up => up.Name.Equals("id", StringComparison.OrdinalIgnoreCase));

            if (idProperty == null || !_waitingCalls.TryRemove(idProperty.Value, out var tcs)) return;

            var success = eventArgs.ApplicationMessage.GetUserProperty<bool>("Success");
            if (success)
            {
                tcs.TrySetResult(eventArgs.ApplicationMessage);
            }
            else
            {
                var code = eventArgs.ApplicationMessage.GetUserProperty<int?>("ErrorCode") ?? 0;
                var message = eventArgs.ApplicationMessage.GetUserProperty("ErrorMessage");
                tcs.TrySetException(new RpcException(message, code));
            }
        }

        private T Deserialize<T>(string contentType, byte[] payload)
        {
            if (!Serializers.TryGetValue(contentType, out var serializer))
            {
                throw new ArgumentOutOfRangeException(nameof(contentType), $"Deserialize error: Invalid content type '{contentType}'.");
            }

            return serializer.Deserialize<T>(payload);
        }
    }
}
