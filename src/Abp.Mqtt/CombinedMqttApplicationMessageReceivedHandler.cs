using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client.Receiving;

namespace Abp.Mqtt
{
    public class CombinedMqttApplicationMessageReceivedHandler : IMqttApplicationMessageReceivedHandler
    {
        private readonly ConcurrentDictionary<int, IMqttApplicationMessageReceivedHandler> _subHandlers;

        public CombinedMqttApplicationMessageReceivedHandler()
        {
            _subHandlers = new ConcurrentDictionary<int, IMqttApplicationMessageReceivedHandler>();
        }

        public CombinedMqttApplicationMessageReceivedHandler(params IMqttApplicationMessageReceivedHandler[] handlers)
        {
            _subHandlers = new ConcurrentDictionary<int, IMqttApplicationMessageReceivedHandler>(
                handlers.Select(handler => new KeyValuePair<int, IMqttApplicationMessageReceivedHandler>(handler.GetHashCode(), handler)));
        }

        public async Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            foreach (var handler in _subHandlers)
            {
                try
                {
                    await handler.Value.HandleApplicationMessageReceivedAsync(eventArgs).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // TODO: Log Exception
                }
            }
        }

        public bool TryAdd(IMqttApplicationMessageReceivedHandler handler)
        {
            return _subHandlers.TryAdd(handler.GetHashCode(), handler);
        }

        public bool TryRemove(IMqttApplicationMessageReceivedHandler handler)
        {
            return _subHandlers.TryRemove(handler.GetHashCode(), out _);
        }
    }
}
