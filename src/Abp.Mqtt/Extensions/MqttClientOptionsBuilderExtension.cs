using MQTTnet.Client;
using MQTTnet.Client.Options;

namespace Abp.Mqtt.Extensions
{
    public static class MqttClientOptionsBuilderExtension
    {
        public static IMqttClientOptions<T> Build<T>(this MqttClientOptionsBuilder builder) where T : IMqttClient
        {
            return MqttClientOptions<T>.Clone((MqttClientOptions) builder.Build());
        }
    }
}
