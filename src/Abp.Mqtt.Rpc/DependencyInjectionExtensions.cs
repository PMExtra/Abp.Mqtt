using Microsoft.Extensions.DependencyInjection;

namespace Abp.Mqtt.Rpc
{
    public static class DependencyInjectionExtensions
    {
        public static RpcServerBuilder AddMqttRpcServer<TServer>(this IServiceCollection serviceCollection)
            where TServer : class, IRpcServer
        {
            serviceCollection.AddSingleton<TServer>();
            var builder = new RpcServerBuilder(serviceCollection);
            return builder;
        }

        public static RpcServerBuilder AddMqttRpcClient(this IServiceCollection serviceCollection)
        {
            return AddMqttRpcClient<RpcClient>(serviceCollection);
        }

        public static RpcServerBuilder AddMqttRpcClient<TClient>(this IServiceCollection serviceCollection)
            where TClient : RpcClient
        {
            serviceCollection.AddSingleton<RpcClient>();
            var builder = new RpcServerBuilder(serviceCollection);
            return builder;
        }
    }
}
