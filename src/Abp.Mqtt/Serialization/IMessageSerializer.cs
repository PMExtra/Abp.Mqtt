using System;

namespace Abp.Mqtt.Serialization
{
    public interface IMessageSerializer
    {
        bool UTF8 { get; }

        string ContentType { get; }

        byte[] Serialize(object payload);

        T Deserialize<T>(byte[] payload);

        object Deserialize(byte[] payload, Type type);
    }
}
