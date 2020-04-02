using System.Collections.Generic;
using Abp.Mqtt.Rpc.Serialization;
using MQTTnet.Extensions.ManagedClient;

namespace Abp.Mqtt.Rpc
{
    public interface IRpcServerOptions
    {
        IManagedMqttClient MqttClient { get; set; }

        List<IMessageSerializer> Serializers { get; set; }
    }

    public interface IRpcServerOptions<T> : IRpcServerOptions where T : IRpcServer
    {
    }
}
