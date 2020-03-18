using System.Linq;
using System.Reflection;
using Abp.Mqtt.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Abp.Mqtt.Rpc
{
    public class RpcServerBuilder
    {
        private readonly IServiceCollection _serviceCollection;

        internal RpcServerBuilder(IServiceCollection serviceCollection)
        {
            _serviceCollection = serviceCollection;
        }

        public RpcServerBuilder AddService<TService>() where TService : class, IRpcService
        {
            _serviceCollection.AddTransient(typeof(IRpcService), typeof(TService));
            return this;
        }

        public RpcServerBuilder AddServices(Assembly assembly)
        {
            var types = assembly.GetExportedTypes().Where(type => type.IsClass && typeof(IRpcService).IsAssignableFrom(type));
            foreach (var type in types)
            {
                _serviceCollection.AddTransient(typeof(IRpcService), type);
                _serviceCollection.AddTransient(type);
            }

            return this;
        }
    }
}
