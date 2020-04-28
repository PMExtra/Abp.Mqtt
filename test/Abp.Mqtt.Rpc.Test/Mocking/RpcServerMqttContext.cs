using System.Diagnostics.CodeAnalysis;
using Abp.Mqtt.Contexts;

namespace Abp.Mqtt.Rpc.Test.Mocking
{
    public class RpcServerMqttContext : ManagedMqttContext
    {
        [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")]
        public RpcServerMqttContext(IManagedMqttContextOptions<RpcServerMqttContext> options) : base(options)
        {
        }
    }
}
