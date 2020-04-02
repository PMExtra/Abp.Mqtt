using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using MQTTnet.Extensions.ManagedClient;

namespace Abp.Mqtt
{
    public class ManagedMqttClientOptions<T> : ManagedMqttClientOptions, IManagedMqttClientOptions<T> where T : IManagedMqttClient
    {
        public static ManagedMqttClientOptions<T> Clone(ManagedMqttClientOptions options)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, options);
                ms.Position = 0;
                return (ManagedMqttClientOptions<T>) formatter.Deserialize(ms);
            }
        }
    }
}
