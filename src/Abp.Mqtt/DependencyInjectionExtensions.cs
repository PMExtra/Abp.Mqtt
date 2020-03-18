using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;

namespace Abp.Mqtt
{
    public static class DependencyInjectionExtensions
    {
        public static MqttConfigurator AddMqtt(this IServiceCollection serviceCollection, string mqttUri)
        {
            var configurator = new MqttConfigurator(mqttUri);
            serviceCollection.AddSingleton(configurator);
            serviceCollection.AddSingleton(services => services.GetService<MqttConfigurator>().ClientOptions);
            serviceCollection.AddSingleton(services => services.GetService<MqttConfigurator>().ManagedClientOptions);
            serviceCollection.AddSingleton(services => services.GetService<MqttConfigurator>().CreateMqttClient().ConfigureAwait(false).GetAwaiter().GetResult());
            serviceCollection.AddSingleton(services => services.GetService<MqttConfigurator>().CreateManagedMqttClient().ConfigureAwait(false).GetAwaiter().GetResult());

            return configurator;
        }

        public static MqttConfigurator AddMqtt(this IWindsorContainer iocContainer, string mqttUri)
        {
            var configurator = new MqttConfigurator(mqttUri);
            iocContainer.Register(Component.For<MqttConfigurator>().Instance(configurator));
            iocContainer.Register(Component.For<IMqttClientOptions>().UsingFactoryMethod(kernel => kernel.Resolve<MqttConfigurator>().ClientOptions).LifestyleSingleton());
            iocContainer.Register(Component.For<IManagedMqttClientOptions>().UsingFactoryMethod(kernel => kernel.Resolve<MqttConfigurator>().ManagedClientOptions)
                .LifestyleSingleton());
            iocContainer.Register(Component.For<IMqttClient>()
                .UsingFactoryMethod(kernel => kernel.Resolve<MqttConfigurator>().CreateMqttClient().ConfigureAwait(false).GetAwaiter().GetResult())
                .LifestyleSingleton());
            iocContainer.Register(Component.For<IManagedMqttClient>()
                .UsingFactoryMethod(kernel => kernel.Resolve<MqttConfigurator>().CreateManagedMqttClient().ConfigureAwait(false).GetAwaiter().GetResult())
                .LifestyleSingleton());

            return configurator;
        }
    }
}
