using MQTTnet.Extensions.ManagedClient;

namespace Abp.Mqtt.Extensions
{
    public static class ManagedMqttClientOptionsBuilderExtension
    {
        public static IManagedMqttClientOptions<T> Build<T>(this ManagedMqttClientOptionsBuilder builder) where T : IManagedMqttClient
        {
            return ManagedMqttClientOptions<T>.Clone(builder.Build());
        }
    }
}
