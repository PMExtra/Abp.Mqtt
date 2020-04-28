using System.Collections.Generic;
using Abp.Mqtt.Serialization;
using MQTTnet.Extensions.ManagedClient;

namespace Abp.Mqtt.Contexts
{
    public class ManagedMqttContextOptions<T> : IManagedMqttContextOptions<T> where T : ManagedMqttContext
    {
        public IManagedMqttClientOptions ManagedMqttClientOptions { get; set; }

        public List<IMessageSerializer> MessageSerializers { get; } = new List<IMessageSerializer>
        {
            new JsonMessageSerializer()
        };
    }
}
