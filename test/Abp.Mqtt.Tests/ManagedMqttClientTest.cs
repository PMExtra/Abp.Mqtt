using System;
using Abp.Dependency;
using MQTTnet.Extensions.ManagedClient;
using Xunit;

namespace Abp.Mqtt.Tests
{
    public class ManagedMqttClientTest : IDisposable
    {
        private const string MqttServer = "mqtt://localhost";

        private readonly IIocManager _iocManagerA;
        private readonly IIocManager _iocManagerB;
        private readonly IManagedMqttClient _clientA;
        private readonly IManagedMqttClient _clientB;

        public ManagedMqttClientTest()
        {
            _iocManagerA = new IocManager();
            _iocManagerB = new IocManager();

            _iocManagerA.IocContainer.AddMqtt(MqttServer);
            _iocManagerB.IocContainer.AddMqtt(MqttServer);

            _clientA = _iocManagerA.Resolve<IManagedMqttClient>();
            _clientB = _iocManagerB.Resolve<IManagedMqttClient>();
        }

        public virtual void Dispose()
        {
            _iocManagerA.Dispose();
            _iocManagerB.Dispose();
        }

        [Fact]
        public void TestStarted()
        {
            Assert.True(_clientA.IsStarted);
            Assert.True(_clientB.IsStarted);
        }

        [Fact]
        public void TestConnected()
        {
            Assert.True(_clientA.IsConnected);
            Assert.True(_clientB.IsConnected);
        }
    }
}
