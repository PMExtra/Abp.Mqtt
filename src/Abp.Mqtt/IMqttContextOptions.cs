using MQTTnet.Client.Options;

namespace Abp.Mqtt
{
    public interface IMqttContextOptions
    {
        IMqttClientOptions MqttClientOptions { get; set; }
    }

    public interface IMqttContextOptions<T> : IMqttContextOptions where T : MqttContext
    {
    }
}
