using System;
using Abp.Mqtt.Rpc;
using Abp.Mqtt.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Formatter;

namespace Abp.Mqtt.Service.Test
{
    internal class Program
    {
        public static readonly IConfigurationRoot AppConfiguration;
        private static readonly string mqttService = "mqtt://packer02:123qwe@192.168.102.101";

        static Program()
        {
            AppConfiguration = new ConfigurationBuilder()
                .Build();
        }

        public static void Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            using var provider = serviceCollection.BuildServiceProvider();
            var server = provider.GetRequiredService<RpcServer>();
            server.Wait();
        }

        public static IServiceCollection ConfigureServices(IServiceCollection services)
        {
            services
                .AddMqtt(mqttService)
                .ConfigureSerializers(serializers =>
                {
                    serializers.Insert(0, new BsonMessageSerializer());
                })
                .ConfigureClient(builder =>
                {
                    builder.WithClientId("packer02");
                    builder.WithProtocolVersion(MqttProtocolVersion.V500);
                })
                .ConfigureManagedClient(builder => { builder.WithAutoReconnectDelay(TimeSpan.FromSeconds(5)); });

            services
                .AddMqttRpcServer<RpcServer>()
                .AddServices(typeof(Program).Assembly);

            return services;
        }
    }
}
