using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client.Receiving;

namespace Abp.Mqtt
{
    public class AwareDistributeMessageReceivedHandler : IMqttApplicationMessageReceivedHandler
    {
        private readonly Func<MqttApplicationMessageReceivedEventArgs, Task> _handleReceivedApplicationMessageAsync;

        public AwareDistributeMessageReceivedHandler(
            Func<MqttApplicationMessageReceivedEventArgs, Task> handleReceivedApplicationMessageAsync)
        {
            _handleReceivedApplicationMessageAsync = handleReceivedApplicationMessageAsync ?? throw new ArgumentNullException(nameof(handleReceivedApplicationMessageAsync));
        }

        public async Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {

            await _handleReceivedApplicationMessageAsync(eventArgs).ConfigureAwait(false);
        }
    }
}
