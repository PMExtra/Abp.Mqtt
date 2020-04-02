using MQTTnet;
using MQTTnet.Client;

namespace Abp.Mqtt
{
    public class MqttContext
    {
        public MqttContext(IMqttContextOptions options)
        {
            MqttClient = new MqttFactory().CreateMqttClient();
            MqttClient.ConnectAsync(options.MqttClientOptions).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public IMqttClient MqttClient { get; }
    }
}
