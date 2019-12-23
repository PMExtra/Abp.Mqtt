using System;
using MQTTnet;

namespace Abp.Mqtt.Serialization
{
    public interface IMessageSerializer
    {
        string ContentType { get; }

        byte[] Serialize(object payload);

        T Deserialize<T>(byte[] payload);

        object Deserialize(byte[] payload, Type type);
    }
}
