using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Diagnostics;

namespace Abp.Mqtt.Contexts
{
    public class MqttContext
    {
        public MqttContext(IMqttContextOptions options)
        {
            Factory = new MqttFactory();
            Logger = Factory.DefaultLogger;
            MqttClient = Factory.CreateMqttClient();
            MqttClient.ConnectAsync(options.MqttClientOptions).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        protected IMqttFactory Factory { get; }

        protected IMqttNetLogger Logger { get; }

        public IMqttClient MqttClient { get; }
    }
}
