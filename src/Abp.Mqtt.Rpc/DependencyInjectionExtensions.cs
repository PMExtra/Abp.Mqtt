using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Abp.Mqtt.Rpc
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddMqttRpcServer(this IServiceCollection serviceCollection, Action<IServiceProvider, IRpcServerOptions> configure)
        {
            serviceCollection.AddSingleton<RpcServer>();
            serviceCollection.AddSingleton<IRpcServerOptions>(services =>
            {
                var options = new RpcServerOptions<RpcServer>();
                configure(services, options);
                return options;
            });
            return serviceCollection;
        }

        public static IServiceCollection AddRpcService<T>(this IServiceCollection serviceCollection) where T : class, IRpcService
        {
            serviceCollection.AddScoped<T>();
            return serviceCollection;
        }

        public static IServiceCollection AddRpcServices(this IServiceCollection serviceCollection, Assembly assembly)
        {
            var types = assembly.GetExportedTypes().Where(type => type.IsClass && typeof(IRpcService).IsAssignableFrom(type));
            foreach (var type in types)
            {
                serviceCollection.AddTransient(typeof(IRpcService), type);
                serviceCollection.AddTransient(type);
            }

            return serviceCollection;
        }

        public static IServiceCollection AddMqttRpcServer<TServer>(this IServiceCollection serviceCollection, Action<IServiceProvider, IRpcServerOptions<TServer>> configure)
            where TServer : class, IRpcServer
        {
            throw new NotImplementedException();
            serviceCollection.AddSingleton<TServer>();
            serviceCollection.AddSingleton<IRpcServerOptions<TServer>>(services =>
            {
                var options = new RpcServerOptions<TServer>();
                configure(services, options);
                return options;
            });
            return serviceCollection;
        }

        public static IServiceCollection AddMqttRpcClient(this IServiceCollection serviceCollection, Action<IServiceProvider, IRpcClientOptions> configure)
        {
            return AddMqttRpcClient<SimpleRpcClient>(serviceCollection, configure);
        }

        public static IServiceCollection AddMqttRpcClient<TClient>(this IServiceCollection serviceCollection, Action<IServiceProvider, IRpcClientOptions<TClient>> configure)
            where TClient : SimpleRpcClient
        {
            serviceCollection.AddSingleton<TClient>();
            serviceCollection.AddSingleton<IRpcClientOptions<TClient>>(services =>
            {
                var options = new RpcClientOptions<TClient>();
                configure(services, options);
                return options;
            });
            return serviceCollection;
        }
    }
}
