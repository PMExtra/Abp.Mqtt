using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Abp.Mqtt.Serialization
{
    public class JsonMessageSerializer : IMessageSerializer
    {
        public string ContentType => "application/json";

        public virtual byte[] Serialize(object payload)
        {
            return payload == null ? null : Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload));
        }

        public virtual T Deserialize<T>(byte[] payload)
        {
            return payload?.Any() == true
                ? JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(payload))
                : default;
        }

        public object Deserialize(byte[] payload, Type type)
        {
            return payload?.Any() == true
                ? JsonConvert.DeserializeObject(Encoding.UTF8.GetString(payload), type)
                : default;
        }
    }
}
