using System.Collections.Immutable;
using Abp.Mqtt.Serialization;
using MQTTnet;
using MQTTnet.Diagnostics;
using MQTTnet.Extensions.ManagedClient;

namespace Abp.Mqtt.Contexts
{
    public class ManagedMqttContext
    {
        public ManagedMqttContext(IManagedMqttContextOptions options)
        {
            Factory = new MqttFactory();
            Logger = Factory.DefaultLogger;
            Serializers = options.MessageSerializers.ToImmutableSortedDictionary(serializer => serializer.ContentType, serializer => serializer);
            ManagedMqttClient = Factory.CreateManagedMqttClient();
            ManagedMqttClient.StartAsync(options.ManagedMqttClientOptions).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public ImmutableSortedDictionary<string, IMessageSerializer> Serializers { get; }

        public IManagedMqttClient ManagedMqttClient { get; }

        public IMqttFactory Factory { get; }

        public IMqttNetLogger Logger { get; }
    }
}
