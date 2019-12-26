using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;

namespace Abp.Mqtt
{
    public class MqttConfigurator
    {
        private readonly List<Action<MqttClientOptionsBuilder>> _clientConfigurators = new List<Action<MqttClientOptionsBuilder>>();
        private readonly List<Action<ManagedMqttClientOptionsBuilder>> _managedClientConfigurators = new List<Action<ManagedMqttClientOptionsBuilder>>();
        private readonly string _mqttUri;
        private IMqttClientOptions _clientOptions;
        private IManagedMqttClientOptions _managedClientOptions;

        public MqttConfigurator(string mqttUri)
        {
            _mqttUri = mqttUri;
        }

        public IMqttClientOptions ClientOptions
        {
            get => _clientOptions ?? (_clientOptions = GetClientOptions());
            set => _clientOptions = value;
        }

        public IManagedMqttClientOptions ManagedClientOptions
        {
            get => _managedClientOptions ?? (_managedClientOptions = GetManagedClientOptions());
            set => _managedClientOptions = value;
        }

        public MqttConfigurator ConfigureClient(Action<MqttClientOptionsBuilder> configure)
        {
            _clientConfigurators.Add(configure);
            _clientOptions = null;
            return this;
        }

        public MqttConfigurator ConfigureManagedClient(Action<ManagedMqttClientOptionsBuilder> configure)
        {
            _managedClientConfigurators.Add(configure);
            _managedClientOptions = null;
            return this;
        }

        protected virtual IMqttClientOptions GetClientOptions()
        {
            var uri = new Uri(_mqttUri);
            var builder = new MqttClientOptionsBuilder();
            var port = uri.IsDefaultPort ? null : (int?)uri.Port;
            switch (uri.Scheme)
            {
                case "tcp":
                case "mqtt":
                    builder.WithTcpServer(uri.Host, port);
                    break;

                case "ssl":
                case "tls":
                    builder.WithTcpServer(uri.Host, port).WithTls();
                    break;

                case "ws":
                case "wss":
                    builder.WithWebSocketServer(uri.ToString());
                    break;

                default:
                    throw new ArgumentException("Unexpected scheme in uri.");
            }
            if (uri.UserInfo?.Any() == true)
            {
                var userInfo = uri.UserInfo.Split(':');
                var username = userInfo[0];
                var password = userInfo.Length > 1 ? userInfo[1] : "";
                builder.WithCredentials(username, password);
            }
            foreach (var configure in _clientConfigurators)
            {
                configure(builder);
            }
            return builder.Build();
        }

        protected virtual IManagedMqttClientOptions GetManagedClientOptions()
        {
            var builder = new ManagedMqttClientOptionsBuilder();
            builder
                .WithClientOptions(ClientOptions);
            foreach (var configure in _managedClientConfigurators)
            {
                configure(builder);
            }
            return builder.Build();
        }

        public async Task<IMqttClient> CreateMqttClient(Func<IMqttClient, Task> configure = null)
        {
            var client = new MqttFactory().CreateMqttClient();
            if (configure != null)
            {
                await configure(client);
            }
            await client.ConnectAsync(ClientOptions);
            return client;
        }

        public async Task<IManagedMqttClient> CreateManagedMqttClient(Func<IManagedMqttClient, Task> configure = null)
        {
            var client = new MqttFactory().CreateManagedMqttClient();
            if (configure != null)
            {
                await configure(client);
            }
            await client.StartAsync(ManagedClientOptions);
            return client;
        }
    }
}
