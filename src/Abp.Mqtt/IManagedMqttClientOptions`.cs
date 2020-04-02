using MQTTnet.Extensions.ManagedClient;

namespace Abp.Mqtt
{
    public interface IManagedMqttClientOptions<T> : IManagedMqttClientOptions where T : IManagedMqttClient
    {
    }
}
