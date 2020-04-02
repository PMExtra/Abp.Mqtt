using System;
using Abp.Mqtt.Extensions;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Diagnostics;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Implementations;

namespace Abp.Mqtt
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddMqttClient(this IServiceCollection serviceCollection, Action<MqttClientOptionsBuilder> configure, ServiceLifetime lifetime)
        {
            var builder = new MqttClientOptionsBuilder();
            configure(builder);
            serviceCollection.AddSingleton(builder.Build());
            serviceCollection.Add(new ServiceDescriptor(typeof(IMqttClient), services =>
            {
                var client = new MqttFactory().CreateMqttClient();
                client.ConnectAsync(services.GetRequiredService<IMqttClientOptions>()).ConfigureAwait(false).GetAwaiter().GetResult();
                return client;
            }, lifetime));

            return serviceCollection;
        }

        public static IServiceCollection AddMqttClient<T>(this IServiceCollection serviceCollection, Action<MqttClientOptionsBuilder> configure, ServiceLifetime lifetime)
            where T : IMqttClient
        {
            throw new NotImplementedException();
            var builder = new MqttClientOptionsBuilder();
            configure(builder);
            serviceCollection.AddSingleton(builder.Build<T>());
            serviceCollection.Add(new ServiceDescriptor(typeof(T), lifetime));

            return serviceCollection;
        }

        public static IServiceCollection AddManagedMqttClient(this IServiceCollection serviceCollection, Action<ManagedMqttClientOptionsBuilder> configure,
            ServiceLifetime lifetime)
        {
            var builder = new ManagedMqttClientOptionsBuilder();
            configure(builder);
            serviceCollection.AddSingleton(builder.Build());
            serviceCollection.Add(new ServiceDescriptor(typeof(IManagedMqttClient), services =>
            {
                var client = new MqttFactory().CreateManagedMqttClient();
                client.StartAsync(services.GetRequiredService<IManagedMqttClientOptions>()).ConfigureAwait(false).GetAwaiter().GetResult();
                return client;
            }, lifetime));

            return serviceCollection;
        }

        public static IServiceCollection AddManagedMqttClient<T>(this IServiceCollection serviceCollection, Action<ManagedMqttClientOptionsBuilder> configure,
            ServiceLifetime lifetime)
            where T : IManagedMqttClient
        {
            throw new NotImplementedException();
            var builder = new ManagedMqttClientOptionsBuilder();
            configure(builder);
            serviceCollection.AddSingleton(builder.Build<T>());
            serviceCollection.Add(new ServiceDescriptor(typeof(T), lifetime));

            return serviceCollection;
        }
    }
}
