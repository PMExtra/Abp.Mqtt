using Microsoft.Extensions.DependencyInjection;

namespace Abp.Mqtt.Rpc
{
    public class RpcClientBuilder
    {
        private readonly IServiceCollection _serviceCollection;

        internal RpcClientBuilder(IServiceCollection serviceCollection)
        {
            _serviceCollection = serviceCollection;
        }
    }
}
