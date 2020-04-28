using System.Collections.Generic;
using Abp.Mqtt.Serialization;
using MQTTnet.Client.Options;

namespace Abp.Mqtt.Contexts
{
    public class MqttContextOptions<T> : IMqttContextOptions<T> where T : MqttContext
    {
        public List<IMessageSerializer> MessageSerializers { get; } = new List<IMessageSerializer>
        {
            new JsonMessageSerializer()
        };

        public IMqttClientOptions MqttClientOptions { get; set; }
    }
}
