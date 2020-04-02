using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Protocol;

namespace Abp.Mqtt.Rpc
{
    public class SimpleRpcClient : RpcClientBase
    {
        [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")]
        public SimpleRpcClient(IRpcClientOptions<SimpleRpcClient> options) : base(options)
        {
        }

        public new Task<T> ExecuteAsync<T>(string methodName, object payload, MqttQualityOfServiceLevel qos, TimeSpan timeout, string target,
            CancellationToken cancellationToken = default)
        {
            return base.ExecuteAsync<T>(methodName, payload, qos, timeout, target, cancellationToken);
        }

        public new Task<T> ExecuteAsync<T>(string methodName, byte[] payload, bool utf8Payload, string contentType, MqttQualityOfServiceLevel qos, TimeSpan timeout,
            string target, CancellationToken cancellationToken = default)
        {
            return base.ExecuteAsync<T>(methodName, payload, utf8Payload, contentType, qos, timeout, target, cancellationToken);
        }

        public new Task<MqttApplicationMessage> ExecuteAsync(string methodName, byte[] payload, bool utf8Payload, string contentType, MqttQualityOfServiceLevel qos,
            TimeSpan timeout, string target, CancellationToken cancellationToken = default)
        {
            return base.ExecuteAsync(methodName, payload, utf8Payload, contentType, qos, timeout, target, cancellationToken);
        }

        public new Task FireAndForgetAsync(string target, string methodName, object args, MqttQualityOfServiceLevel qos, TimeSpan timeout = default)
        {
            return base.FireAndForgetAsync(target, methodName, args, qos, timeout);
        }

        public new Task FireAndForgetAsync(string target, string methodName, byte[] payload, bool utf8Payload, string contentType, MqttQualityOfServiceLevel qos,
            TimeSpan timeout = default)
        {
            return base.FireAndForgetAsync(target, methodName, payload, utf8Payload, contentType, qos, timeout);
        }
    }
}
