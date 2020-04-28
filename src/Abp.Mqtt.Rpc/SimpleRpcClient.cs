using System;
using System.Threading;
using System.Threading.Tasks;
using Abp.Mqtt.Contexts;
using MQTTnet;
using MQTTnet.Protocol;

namespace Abp.Mqtt.Rpc
{
    public class SimpleRpcClient : RpcClientBase
    {
        public SimpleRpcClient(ManagedMqttContext context) : base(context)
        {
        }

        public new Task ExecuteAsync(string target, string methodName, object payload, MqttQualityOfServiceLevel qos, TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            return base.ExecuteAsync(target, methodName, payload, qos, timeout, cancellationToken);
        }

        public new Task<T> ExecuteAsync<T>(string target, string methodName, object payload, MqttQualityOfServiceLevel qos, TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            return base.ExecuteAsync<T>(target, methodName, payload, qos, timeout, cancellationToken);
        }

        public new Task<T> ExecuteAsync<T>(string target, string methodName, byte[] payload, bool utf8Payload, string contentType, MqttQualityOfServiceLevel qos, TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            return base.ExecuteAsync<T>(target, methodName, payload, utf8Payload, contentType, qos, timeout, cancellationToken);
        }

        public new Task<MqttApplicationMessage> ExecuteAsync(string target, string methodName, byte[] payload, bool utf8Payload, string contentType, MqttQualityOfServiceLevel qos,
            TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            return base.ExecuteAsync(target, methodName, payload, utf8Payload, contentType, qos, timeout, cancellationToken);
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
