using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace Abp.Mqtt.Serialization
{
    public class BsonMessageSerializer : IMessageSerializer
    {
        public string ContentType => "application/bson";

        public virtual byte[] Serialize(object payload)
        {
            if (payload == null) return null;

            if (IsSimpleType(payload.GetType())) payload = BsonDataWrapper.Wrap(payload);

            using (var ms = new MemoryStream())
            using (var writer = new BsonDataWriter(ms))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(writer, payload);
                return ms.ToArray();
            }
        }

        public virtual T Deserialize<T>(byte[] payload)
        {
            if (payload?.Any() != true) return default;

            if (IsSimpleType(typeof(T))) return (T) Deserialize(payload, typeof(T));

            using (var ms = new MemoryStream(payload))
            using (var reader = new BsonDataReader(ms))
            {
                var serializer = new JsonSerializer();
                return serializer.Deserialize<T>(reader);
            }
        }

        public object Deserialize(byte[] payload, Type type)
        {
            if (payload?.Any() != true) return default;

            var isSimpleType = IsSimpleType(type);
            if (isSimpleType) type = typeof(BsonDataWrapper<>).MakeGenericType(type);

            using (var ms = new MemoryStream(payload))
            using (var reader = new BsonDataReader(ms))
            {
                var serializer = new JsonSerializer();
                var data = serializer.Deserialize(reader, type);
                if (isSimpleType)
                {
                    var property = type.GetProperty("Data");
                    Debug.Assert(property != null, nameof(property) + " != null");
                    data = property.GetValue(data);
                }

                return data;
            }
        }

        private bool IsSimpleType(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                case TypeCode.Char:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                case TypeCode.DateTime:
                case TypeCode.String:
                    return true;

                default:
                    return false;
            }
        }
    }
}
