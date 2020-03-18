using Microsoft.Extensions.DependencyInjection;
using System;
using Abp.Mqtt.Rpc;
using Abp.Mqtt.Serialization;
using MQTTnet.Formatter;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using MQTTnet.Protocol;

namespace Abp.Mqtt.Client.Test
{
    class Program
    {
        private static IServiceProvider _serviceProvider;
        private static string mqttService = "mqtt://packer01:123qwe@192.168.102.101";

        static async Task Main(string[] args)
        {


            _serviceProvider = ConfigureServices(new ServiceCollection()).BuildServiceProvider();



            int requestType = 1;

            do
            {

                Console.WriteLine("plase press any key contine exit step ...");
                Console.ReadKey();

                try
                {

                    if (requestType == 1)
                    {
                        int parallel = 2;

                        var period = TimeSpan.FromMinutes(1);

                        var packerId = "packer02";

                        var success = 0;
                        var failed = 0;
                        var id = 0;
                        var client = _serviceProvider.GetRequiredService<RpcClient>();

                        using (var cts = new CancellationTokenSource(period))
                        {

                            var tasks = Enumerable.Range(0, parallel).Select(async _ =>
                            {
                                while (!cts.Token.IsCancellationRequested)
                                {
                                    try
                                    {
                                        var beginDate = DateTime.Now;
                                       
                                        var pong = await client.ExecuteAsync<string>("Ping", "Ping", MqttQualityOfServiceLevel.ExactlyOnce, TimeSpan.FromMinutes(1), packerId,
                                            cts.Token);
                                        if (pong == "Pong")
                                        {
                                            var endDate = DateTime.Now;
                                            Console.WriteLine(" "+beginDate.ToString("HH:mm:ss.fff")+" -> "+ endDate.ToString(endDate.ToString("HH:mm:ss.fff")));

                                            Interlocked.Increment(ref success);
                                        }
                                        else
                                        {
                                            throw new Exception();
                                        }
                                    }
                                    catch
                                    {
                                        Interlocked.Increment(ref failed);
                                    }
                                }
                            }).ToList();
                            await Task.WhenAll(tasks);
                        }

                        Console.WriteLine($"Finished {success} + {failed} Ping / Pong in 1 minute with {parallel} threads.");
                    }
                    else
                    {
                        var packerId = "packer02";
                        var client = _serviceProvider.GetRequiredService<RpcClient>();
                        Console.WriteLine("启动");
                        Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff"));
                        var pong = await client.ExecuteAsync<string>("Ping", "Ping", MqttQualityOfServiceLevel.ExactlyOnce, TimeSpan.FromMinutes(1), packerId);
                        Console.WriteLine("server respones -> " + DateTime.Now.ToString("HH:mm:ss.fff"));
                        Console.WriteLine(pong);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

            } while (true);

            Console.Read();

        }

        private static IServiceCollection ConfigureServices(IServiceCollection services)
        {
            services.AddMqtt(mqttService)
                .ConfigureClient(builder =>
                {
                    builder.WithProtocolVersion(MqttProtocolVersion.V500);
                })
                .ConfigureManagedClient(builder =>
                {
                    builder.WithAutoReconnectDelay(TimeSpan.FromSeconds(5));
                });

            services.AddMqttRpcClient().AddSerializer<BsonMessageSerializer>();

            return services;
        }
    }
}
