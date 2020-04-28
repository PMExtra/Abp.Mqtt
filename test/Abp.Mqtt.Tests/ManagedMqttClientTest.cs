using System;
using Abp.Mqtt.Contexts;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Extensions;
using Xunit;

namespace Abp.Mqtt.Tests
{
    public class ManagedMqttClientTest
    {
        public class MqttContextA : MqttContext
        {
            public MqttContextA(IMqttContextOptions<MqttContextA> options) : base(options)
            {
            }
        }

        public class MqttContextB : MqttContextA
        {
            public MqttContextB(IMqttContextOptions<MqttContextB> options) : base(options)
            {
            }
        }

        public class ManagedMqttContextA : ManagedMqttContext
        {
            public ManagedMqttContextA(IManagedMqttContextOptions<ManagedMqttContextA> options) : base(options)
            {
            }
        }

        public class ManagedMqttContextB : ManagedMqttContextA
        {
            public ManagedMqttContextB(IManagedMqttContextOptions<ManagedMqttContextB> options) : base(options)
            {
            }
        }

        private const string MqttServer = "mqtt://qomo:qomo123@photon";

        [Fact]
        public void TestConnected()
        {
            using var services = new ServiceCollection()
                .AddMqttClient<MqttContextA>(builder =>
                {
                    builder
                        .WithConnectionUri(MqttServer)
                        .WithClientId("ClientA");
                })
                .AddMqttClient<MqttContextB>(builder =>
                {
                    builder.WithConnectionUri(MqttServer)
                        .WithClientId("ClientB");
                })
                .BuildServiceProvider();

            var clientA = services.GetRequiredService<MqttContextA>().MqttClient;
            var clientB = services.GetRequiredService<MqttContextB>().MqttClient;

            Assert.True(clientA.IsConnected);
            Assert.Equal("ClientA", clientA.Options.ClientId);
            Assert.True(clientB.IsConnected);
            Assert.Equal("ClientB", clientB.Options.ClientId);
        }

        [Fact]
        public void TestStarted()
        {
            using var services = new ServiceCollection()
                .AddManagedMqttClient<ManagedMqttContextA>(builder =>
                {
                    builder
                        .WithClientOptions(builder2 =>
                        {
                            builder2
                                .WithConnectionUri(MqttServer)
                                .WithClientId("ClientA");
                        })
                        .WithAutoReconnectDelay(TimeSpan.FromSeconds(5));
                })
                .AddManagedMqttClient<ManagedMqttContextB>(builder =>
                {
                    builder
                        .WithClientOptions(builder2 =>
                        {
                            builder2
                                .WithConnectionUri(MqttServer)
                                .WithClientId("ClientB");
                        })
                        .WithAutoReconnectDelay(TimeSpan.FromSeconds(5));
                })
                .BuildServiceProvider();

            var clientA = services.GetRequiredService<ManagedMqttContextA>().ManagedMqttClient;
            var clientB = services.GetRequiredService<ManagedMqttContextB>().ManagedMqttClient;

            Assert.True(clientA.IsStarted);
            Assert.Equal("ClientA", clientA.Options.ClientOptions.ClientId);
            Assert.True(clientB.IsStarted);
            Assert.Equal("ClientB", clientB.Options.ClientOptions.ClientId);
        }
    }
}
