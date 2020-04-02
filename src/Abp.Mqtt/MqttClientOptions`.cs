using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using MQTTnet.Client;
using MQTTnet.Client.Options;

namespace Abp.Mqtt
{
    public class MqttClientOptions<T> : MqttClientOptions, IMqttClientOptions<T> where T : IMqttClient
    {
        public static MqttClientOptions<T> Clone(MqttClientOptions options)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, options);
                ms.Position = 0;
                return (MqttClientOptions<T>) formatter.Deserialize(ms);
            }
        }
    }
}
