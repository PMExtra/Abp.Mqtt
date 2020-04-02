using MQTTnet.Client;
using MQTTnet.Client.Options;

namespace Abp.Mqtt
{
    public interface IMqttClientOptions<T> : IMqttClientOptions where T : IMqttClient
    {
    }
}
