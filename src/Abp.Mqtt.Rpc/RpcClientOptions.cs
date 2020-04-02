using System.Collections.Generic;
using Abp.Mqtt.Rpc.Serialization;
using MQTTnet.Extensions.ManagedClient;

namespace Abp.Mqtt.Rpc
{
    public class RpcClientOptions<T> : IRpcClientOptions<T> where T : RpcClientBase
    {
        public IManagedMqttClient MqttClient { get; set; }

        public List<IMessageSerializer> Serializers { get; set; } = new List<IMessageSerializer>();
    }
}
