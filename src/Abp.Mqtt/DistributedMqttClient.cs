using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client.Receiving;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;

namespace Abp.Mqtt
{
    public class DistributedMqttClient: IMqttApplicationMessageReceivedHandler
    {
        private readonly IManagedMqttClient _managedMqttClient;
        private Dictionary<string, AwareDistributeMessageReceivedHandler> _messageReceivedHandlers = new Dictionary<string, AwareDistributeMessageReceivedHandler>();
        private readonly object _locker = new object();
        public DistributedMqttClient(IManagedMqttClient managedMqttClient)
        {
            _managedMqttClient = managedMqttClient ?? throw new ArgumentNullException(nameof(managedMqttClient));

            _managedMqttClient.ApplicationMessageReceivedHandler = this;
        }

        public async Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            if (_messageReceivedHandlers.Count == 0) return;

            if (_messageReceivedHandlers.ContainsKey(eventArgs.ApplicationMessage.Topic))
                await _messageReceivedHandlers[eventArgs.ApplicationMessage.Topic].HandleApplicationMessageReceivedAsync(eventArgs).ConfigureAwait(false);
        }

        public async Task SubscribeAsync(string topic, MqttQualityOfServiceLevel qos, AwareDistributeMessageReceivedHandler messageReceivedHandler)
        {
            if(string.IsNullOrEmpty(topic)) throw  new ArgumentNullException("Topic are not set.");

            if(messageReceivedHandler==null) throw new ArgumentNullException(nameof(messageReceivedHandler));

            var isNeedSubscribe = false;
            lock (_locker)
            {
                if (!_messageReceivedHandlers.ContainsKey(topic))
                {
                    _messageReceivedHandlers.Add(topic, messageReceivedHandler);
                    isNeedSubscribe = true;
                }
            }
            if(isNeedSubscribe)
                await _managedMqttClient.SubscribeAsync(topic, qos).ConfigureAwait(false);
        }

        public async Task UnSubscribeAsync(string topic)
        {
            if (string.IsNullOrEmpty(topic)) throw new ArgumentNullException("Topic are not set.");

            lock (_locker)
            {
                _messageReceivedHandlers.Remove(topic);
            }

            await _managedMqttClient.UnsubscribeAsync(topic).ConfigureAwait(false);

        }




    }
}
