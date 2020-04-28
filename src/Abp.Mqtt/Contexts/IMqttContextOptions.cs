using System.Collections.Generic;
using Abp.Mqtt.Serialization;
using MQTTnet.Client.Options;

namespace Abp.Mqtt.Contexts
{
    public interface IMqttContextOptions
    {
        List<IMessageSerializer> MessageSerializers { get; }

        IMqttClientOptions MqttClientOptions { get; }
    }

    public interface IMqttContextOptions<out T> : IMqttContextOptions where T : MqttContext
    {
    }
}
