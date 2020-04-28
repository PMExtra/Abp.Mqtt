using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Mqtt.Contexts;
using Microsoft.Extensions.DependencyInjection;

namespace Abp.Mqtt.Rpc.Test.Mocking
{
    public class RpcService : IRpcService
    {
        private readonly IServiceProvider _serviceProvider;

        public RpcService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public RpcContext CurrentContext { get; set; }

        public string Ping(string ping)
        {
            if (ping != "Ping") throw new ArgumentException("Unexpected argument.");
            return "Pong";
        }

        public async Task Sleep(int seconds)
        {
            await Task.Delay(TimeSpan.FromSeconds(seconds));
        }

        // Also test for method rename feature
        [RpcMethod("GetDefaultSerializer")]
        public string DependencyInjectionTest()
        {
            var options = _serviceProvider.GetRequiredService<IManagedMqttContextOptions<RpcServerMqttContext>>();
            return options.MessageSerializers.First().ContentType;
        }

        public string WhoAmI()
        {
            return CurrentContext.RemoteClientId;
        }
    }
}
