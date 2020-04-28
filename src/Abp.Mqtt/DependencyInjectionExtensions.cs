using System;
using Abp.Mqtt.Contexts;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;

namespace Abp.Mqtt
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddMqttClient<TContext>(this IServiceCollection serviceCollection, Action<MqttClientOptionsBuilder> configure,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TContext : MqttContext
        {
            var builder = new MqttClientOptionsBuilder();
            configure(builder);
            serviceCollection.AddSingleton<IMqttContextOptions<TContext>>(new MqttContextOptions<TContext>
            {
                MqttClientOptions = builder.Build()
            });
            serviceCollection.Add(new ServiceDescriptor(typeof(TContext), typeof(TContext), lifetime));

            return serviceCollection;
        }

        public static IServiceCollection AddManagedMqttClient<TContext>(this IServiceCollection serviceCollection, Action<ManagedMqttClientOptionsBuilder> configure,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TContext : ManagedMqttContext
        {
            var builder = new ManagedMqttClientOptionsBuilder();
            configure(builder);
            serviceCollection.AddSingleton<IManagedMqttContextOptions<TContext>>(new ManagedMqttContextOptions<TContext>
            {
                ManagedMqttClientOptions = builder.Build()
            });
            serviceCollection.Add(new ServiceDescriptor(typeof(TContext), typeof(TContext), lifetime));

            return serviceCollection;
        }
    }
}
