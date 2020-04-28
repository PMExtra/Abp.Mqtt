using System.Diagnostics.CodeAnalysis;
using Abp.Mqtt.Contexts;

namespace Abp.Mqtt.Rpc.Test.Mocking
{
    public class RpcClientMqttContext : ManagedMqttContext
    {
        [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")]
        public RpcClientMqttContext(IManagedMqttContextOptions<RpcClientMqttContext> options) : base(options)
        {
        }
    }
}
