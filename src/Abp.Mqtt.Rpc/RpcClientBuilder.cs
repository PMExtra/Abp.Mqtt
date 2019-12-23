using Abp.Mqtt.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Abp.Mqtt.Rpc
{
    public class RpcClientBuilder
    {
        private readonly IServiceCollection _serviceCollection;

        internal RpcClientBuilder(IServiceCollection serviceCollection)
        {
            _serviceCollection = serviceCollection;
        }

        public RpcClientBuilder AddJsonSerializer()
        {
            _serviceCollection.Replace(ServiceDescriptor.Transient(typeof(IMessageSerializer), typeof(JsonMessageSerializer)));
            return this;
        }
    }
}
