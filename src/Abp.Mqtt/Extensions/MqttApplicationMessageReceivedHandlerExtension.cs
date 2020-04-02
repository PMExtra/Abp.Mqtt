using MQTTnet.Client.Receiving;

namespace Abp.Mqtt.Extensions
{
    public static class MqttApplicationMessageReceivedHandlerExtension
    {
        public static IMqttApplicationMessageReceivedHandler Combine(this IMqttApplicationMessageReceivedHandler origin, IMqttApplicationMessageReceivedHandler @new)
        {
            if (origin == null)
            {
                return @new;
            }

            if (origin is CombinedMqttApplicationMessageReceivedHandler combined)
            {
                combined.TryAdd(@new);
                return combined;
            }

            return new CombinedMqttApplicationMessageReceivedHandler(origin, @new);
        }
    }
}
