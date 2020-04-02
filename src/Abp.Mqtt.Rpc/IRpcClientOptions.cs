using System.Collections.Generic;
using Abp.Mqtt.Rpc.Serialization;
using MQTTnet.Extensions.ManagedClient;

namespace Abp.Mqtt.Rpc
{
    public interface IRpcClientOptions
    {
        IManagedMqttClient MqttClient { get; set; }

        List<IMessageSerializer> Serializers { get; set; }
    }

    public interface IRpcClientOptions<T> : IRpcClientOptions where T : RpcClientBase
    {
    }
}
