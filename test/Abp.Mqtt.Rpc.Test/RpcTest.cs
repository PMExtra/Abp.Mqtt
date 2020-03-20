using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Abp.Mqtt.Serialization;
using Microsoft.Extensions.DependencyInjection;
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
            _serviceProvider = ConfigureServices(new ServiceCollection()).BuildServiceProvider();

            var service = _serviceProvider.GetRequiredService<RpcServer>();
            Task.Run(() => { service.Wait(); });
        }

        private readonly IServiceProvider _serviceProvider;
        private readonly string mqttService = "mqtt://rpcClient:123qwe@192.168.102.101";
        private readonly ITestOutputHelper _testOutputHelper;

        private IServiceCollection ConfigureServices(IServiceCollection services)
        {
            var mqttConfigure =   services.AddMqtt(mqttService)
                .ConfigureClient(builder =>
                {
                    builder.WithClientId("packer01").WithProtocolVersion(MqttProtocolVersion.V500);
                })
                .ConfigureManagedClient(builder => { builder.WithAutoReconnectDelay(TimeSpan.FromSeconds(5)); });

            //mqttConfigure.ConfigureSerializers(configure => { configure.Insert(0, new JsonMessageSerializer()); });

            services.AddMqttRpcClient();

            services.AddMqttRpcServer<RpcServer>();

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
            var client = _serviceProvider.GetRequiredService<RpcClient>();

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
            var packerId = "packer01";
            var client = _serviceProvider.GetRequiredService<RpcClient>();
            var beginDate = DateTime.Now;
            var pong = await client.ExecuteAsync<string>("Ping", "Ping", MqttQualityOfServiceLevel.ExactlyOnce, TimeSpan.FromMinutes(1), packerId);
            var endDate = DateTime.Now;
            _testOutputHelper.WriteLine(beginDate.ToString("HH:mm:ss.fff") + " -> " + endDate.ToString(endDate.ToString("HH:mm:ss.fff")));
            _testOutputHelper.WriteLine("server respones -> " + DateTime.Now.ToString("HH:mm:ss.fff"));
            Assert.Equal("Pong", pong);
        }
    }
}
