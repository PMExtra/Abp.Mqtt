namespace Abp.Mqtt.Serialization
{
    public static class BsonDataWrapper
    {
        public static BsonDataWrapper<T> Wrap<T>(T value)
        {
            return new BsonDataWrapper<T>(value);
        }
    }

    public class BsonDataWrapper<T>
    {
        public BsonDataWrapper(T data)
        {
            Data = data;
        }

        public T Data { get; set; }

        public static implicit operator T(BsonDataWrapper<T> value)
        {
            return value.Data;
        }

        public static implicit operator BsonDataWrapper<T>(T value)
        {
            return new BsonDataWrapper<T>(value);
        }
    }
}
