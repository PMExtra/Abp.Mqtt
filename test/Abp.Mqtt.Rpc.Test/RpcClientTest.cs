using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Abp.Mqtt.Rpc.Test.Mocking;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Extensions;
using MQTTnet.Protocol;
using Xunit;
using Xunit.Abstractions;

namespace Abp.Mqtt.Rpc.Test
{
    public class RpcClientTest
    {
        public RpcClientTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _services = ConfigureServices(new ServiceCollection()).BuildServiceProvider();

            var context = _services.GetRequiredService<RpcServerMqttContext>();
            var server = new RpcServer(context, _services);
            Task.Run(() => { server.Wait(); });

            Thread.Sleep(3000);
        }

        public const string ConnectionUri = "mqtt://qomo:qomo123@photon";
        public const string RpcServerId = "Cloud.Rpc";
        public const string RpcClientId = "B42E996BB86E.Main";

        private readonly IServiceProvider _services;
        private readonly ITestOutputHelper _testOutputHelper;

        private IServiceCollection ConfigureServices(IServiceCollection services)
        {
            services.AddManagedMqttClient<RpcServerMqttContext>(builder =>
            {
                builder.WithClientOptions(builder2 =>
                {
                    builder2
                        .WithConnectionUri(ConnectionUri)
                        .WithClientId(RpcServerId);
                });
            });

            services.AddManagedMqttClient<RpcClientMqttContext>(builder =>
            {
                builder.WithClientOptions(builder2 =>
                {
                    builder2
                        .WithConnectionUri(ConnectionUri)
                        .WithClientId(RpcClientId);
                });
            });
            
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
            var client = _services.GetRequiredService<SimpleRpcClient>();

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
                    var failedCount = 0;
                    while (!cts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            var beginDate = DateTime.Now;
                            var id = parallel + "-" + beginDate.Ticks;
                            var pong = await client.ExecuteAsync<string>(packerId, "Ping", id, MqttQualityOfServiceLevel.ExactlyOnce, TimeSpan.FromMinutes(1), cts.Token);
                            var endDate = DateTime.Now;
                            var d = endDate - beginDate;
                            //_testOutputHelper.WriteLine("id=" + id + " "+beginDate.ToString("HH:mm:ss.fff")+" -> "+ endDate.ToString(endDate.ToString("HH:mm:ss.fff")));
                            if (pong == "Pong")
                            {
                                var castTime = d.Ticks / 10000;
                                if (castTime > maxTime)
                                {
                                    maxTime = castTime;
                                }

                                if (minTime == 0 || castTime < minTime)
                                {
                                    minTime = castTime;
                                }

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
                            failedCount++;
                            Interlocked.Increment(ref failed);
                        }
                    }

                    avgTime = totalCastTime / successCount;
                    _testOutputHelper.WriteLine(
                        $"single MaxTime:{maxTime}+MinTime:{minTime}+AvgTime:{avgTime},  Finished {successCount} + {failedCount} Ping / Pong in 1 minute with {threadId}.");
                }).ToList();
                await Task.WhenAll(tasks);
            }

            _testOutputHelper.WriteLine($"total Finished {success} + {failed} Ping / Pong in 1 minute with {parallel} threads.");
        }

        public class RevClass
        {
            public string reply { get; set; }
        }

        [Fact]
        public async Task TestBoradcast()
        {
            var packerId = "packer02";
            var client = _services.GetRequiredService<SimpleRpcClient>();
            //var pong = await client.ExecuteBoradcastAsync<string>("Ping", Encoding.UTF8.GetBytes("Ping"), "application/bson", MqttQualityOfServiceLevel.AtLeastOnce, TimeSpan.FromMinutes(1),
            //    2);
            //Xunit.Assert.Equal("Pong", pong.ToString());
        }

        [Fact]
        public async Task TestMethod1()
        {
            var packerId = "packer01";
            var client = _services.GetRequiredService<SimpleRpcClient>();
            var beginDate = DateTime.Now;
            var pong = await client.ExecuteAsync<string>(packerId, "Test", new {id = 1}, MqttQualityOfServiceLevel.AtLeastOnce, TimeSpan.FromMinutes(1));
            var endDate = DateTime.Now;
            _testOutputHelper.WriteLine(beginDate.ToString("HH:mm:ss.fff") + " -> ---");
            //Xunit.Assert.Equal("Pong", pong);
        }
    }
}
