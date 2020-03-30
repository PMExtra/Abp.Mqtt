using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Abp.Mqtt.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using Xunit;
using Xunit.Abstractions;

namespace Abp.Mqtt.Rpc.Test
{
    public class RpcTest
    {
        public RpcTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _serviceProviderClient = ConfigureServicesToClient(new ServiceCollection()).BuildServiceProvider();
            var serviceClient = _serviceProviderClient.GetRequiredService<RpcServer>();
            Task.Run(() => { serviceClient.Wait(); });

            _serviceProviderService = ConfigureServicesToServvice(new ServiceCollection()).BuildServiceProvider();
            var serviceService = _serviceProviderService.GetRequiredService<RpcServer>();
            Task.Run(() => { serviceService.Wait(); });
        }

        private readonly IServiceProvider _serviceProviderClient;
        private readonly IServiceProvider _serviceProviderService;
        private readonly string mqttService = "mqtt://rpcClient:123qwe@192.168.102.101";
        private readonly ITestOutputHelper _testOutputHelper;

        private IServiceCollection ConfigureServicesToClient(IServiceCollection services)
        {
            var mqttConfigure =   services.AddMqtt(mqttService)
                .ConfigureClient(builder =>
                {
                    builder.WithClientId("client01").WithProtocolVersion(MqttProtocolVersion.V500);
                })
                .ConfigureManagedClient(builder => { builder.WithAutoReconnectDelay(TimeSpan.FromSeconds(5)); });

            //mqttConfigure.ConfigureSerializers(configure => { configure.Insert(0, new JsonMessageSerializer()); });

            services.AddMqttRpcClient();

            services.AddMqttRpcServer<RpcServer>().AddServices(typeof(RpcService).Assembly);

            return services;
        }

        private IServiceCollection ConfigureServicesToServvice(IServiceCollection services)
        {
            var mqttConfigure = services.AddMqtt(mqttService)
                .ConfigureClient(builder =>
                {
                    builder.WithClientId("service01").WithProtocolVersion(MqttProtocolVersion.V500);
                })
                .ConfigureManagedClient(builder => { builder.WithAutoReconnectDelay(TimeSpan.FromSeconds(5)); });

            //mqttConfigure.ConfigureSerializers(configure => { configure.Insert(0, new JsonMessageSerializer()); });

            services.AddMqttRpcClient();

            services.AddMqttRpcServer<RpcServer>().AddServices(typeof(RpcService).Assembly);

            return services;
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        public async Task PerformanceBenchmark(int parallel)
        {
            var period = TimeSpan.FromMinutes(1);

            var packerId = "packer02";

            var success = 0;
            var failed = 0;
            var i = 0;
            var client = _serviceProviderClient.GetRequiredService<RpcClient>();

            using (var cts = new CancellationTokenSource(period))
            {
                var tasks = Enumerable.Range(0, parallel).Select(async _ =>
                {
                    var threadId = i++;
                    long minTime = 0;
                    long maxTime = 0;
                    long totalCastTime = 0;
                    long avgTime = 0;
                    var successCount = 0;
                    var faildCount = 0;
                    while (!cts.Token.IsCancellationRequested)
                        try
                        {
                            var beginDate = DateTime.Now;
                            var id = parallel + "-" + beginDate.Ticks;
                            var pong = await client.ExecuteAsync<string>("Ping", id, MqttQualityOfServiceLevel.ExactlyOnce, TimeSpan.FromMinutes(1), packerId, cts.Token);
                            var endDate = DateTime.Now;
                            var d = endDate - beginDate;
                            //_testOutputHelper.WriteLine("id=" + id + " "+beginDate.ToString("HH:mm:ss.fff")+" -> "+ endDate.ToString(endDate.ToString("HH:mm:ss.fff")));
                            if (pong == "Pong")
                            {
                                var castTime = d.Ticks / 10000;
                                if (castTime > maxTime)
                                    maxTime = castTime;
                                if (minTime == 0 || castTime < minTime)
                                    minTime = castTime;
                                totalCastTime += castTime;
                                successCount++;
                                _testOutputHelper.WriteLine(castTime.ToString());
                                Interlocked.Increment(ref success);
                            }
                            else
                            {
                                throw new Exception();
                            }
                        }
                        catch
                        {
                            faildCount++;
                            Interlocked.Increment(ref failed);
                        }

                    avgTime = totalCastTime / successCount;
                    _testOutputHelper.WriteLine(
                        $"single MaxTime:{maxTime}+MinTime:{minTime}+AvgTime:{avgTime},  Finished {successCount} + {faildCount} Ping / Pong in 1 minute with {threadId}.");
                }).ToList();
                await Task.WhenAll(tasks);
            }

            _testOutputHelper.WriteLine($"total Finished {success} + {failed} Ping / Pong in 1 minute with {parallel} threads.");
        }

        [Fact]
        public async Task TestMethod1()
        {
            Thread.Sleep(2000);

            var serviceId = "service01";
            var c_server = _serviceProviderClient.GetRequiredService<RpcServer>();
            await c_server.Subscription(serviceId).ConfigureAwait(false);
            var client = _serviceProviderClient.GetRequiredService<RpcClient>();
            await client.Subscription(serviceId).ConfigureAwait(false);


            var clientId = "client01";
            var s_service = _serviceProviderService.GetRequiredService<RpcServer>();
            await s_service.Subscription(clientId).ConfigureAwait(false);
            var s_client = _serviceProviderService.GetRequiredService<RpcClient>();
            await s_client.Subscription(clientId).ConfigureAwait(false);

            Thread.Sleep(2000);

            var pong = await client.ExecuteAsync<string>("Ping", "Ping", MqttQualityOfServiceLevel.ExactlyOnce, TimeSpan.FromMinutes(1), serviceId);

            _testOutputHelper.WriteLine("client To service -> Pong", pong);

            pong = await s_client.ExecuteAsync<string>("Ping", "Ping", MqttQualityOfServiceLevel.ExactlyOnce, TimeSpan.FromMinutes(1), clientId);

            _testOutputHelper.WriteLine("service To client -> Pong", pong);

        }
    }
}
