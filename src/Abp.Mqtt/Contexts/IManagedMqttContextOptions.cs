using System.Collections.Generic;
using Abp.Mqtt.Serialization;
using MQTTnet.Extensions.ManagedClient;

namespace Abp.Mqtt.Contexts
{
    public interface IManagedMqttContextOptions
    {
        IManagedMqttClientOptions ManagedMqttClientOptions { get; }

        List<IMessageSerializer> MessageSerializers { get; }
    }

    public interface IManagedMqttContextOptions<out T> : IManagedMqttContextOptions where T : ManagedMqttContext
    {
    }
}
