using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Abp.Mqtt.Rpc;
using Abp.Mqtt;
using Abp.Mqtt.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using Xunit;
using Xunit.Abstractions;

namespace Abp.Mqtt.Rpc.Test
{
    public class ClientTest
    {
        private readonly IServiceProvider _serviceProvider;
        private string mqttService = "mqtt://packer01:123qwe@192.168.102.101";
        private readonly ITestOutputHelper _testOutputHelper;
        public ClientTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _serviceProvider = ConfigureServices(new ServiceCollection()).BuildServiceProvider();
        }
        private IServiceCollection ConfigureServices(IServiceCollection services)
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
            int i = 0;
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
                    int successCount = 0;
                    int faildCount = 0;
                    while (!cts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            var beginDate = DateTime.Now;
                            var id = parallel + "-" + beginDate.Ticks;
                            var pong = await client.ExecuteAsync<string>("Ping", id, MqttQualityOfServiceLevel.ExactlyOnce, TimeSpan.FromMinutes(1), packerId, cts.Token);
                            var endDate = DateTime.Now;
                            TimeSpan d = endDate - beginDate;
                            //_testOutputHelper.WriteLine("id=" + id + " "+beginDate.ToString("HH:mm:ss.fff")+" -> "+ endDate.ToString(endDate.ToString("HH:mm:ss.fff")));
                            if (pong == "Pong")
                            {
                                var castTime = d.Ticks / 10000;
                                if (castTime > maxTime)
                                    maxTime = castTime;
                                if (minTime == 0 || castTime < minTime )
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
                    }
                    avgTime = totalCastTime / successCount;
                    _testOutputHelper.WriteLine($"single MaxTime:{maxTime}+MinTime:{minTime}+AvgTime:{avgTime},  Finished {successCount} + {faildCount} Ping / Pong in 1 minute with {threadId}.");

                }).ToList();
                await Task.WhenAll(tasks);
            }

            _testOutputHelper.WriteLine($"total Finished {success} + {failed} Ping / Pong in 1 minute with {parallel} threads.");
        }

        [Fact]
        public async Task TestMethod1()
        {
            var packerId = "packer02";
            var client = _serviceProvider.GetRequiredService<RpcClient>();
            var beginDate = DateTime.Now;
            var pong = await client.ExecuteAsync<string>("Ping", "Ping", MqttQualityOfServiceLevel.ExactlyOnce, TimeSpan.FromMinutes(1), packerId);
            var endDate = DateTime.Now;
            _testOutputHelper.WriteLine(beginDate.ToString("HH:mm:ss.fff") + " -> " + endDate.ToString(endDate.ToString("HH:mm:ss.fff"))); _testOutputHelper.WriteLine("server respones -> " + DateTime.Now.ToString("HH:mm:ss.fff"));
            Xunit.Assert.Equal("Pong", pong);
        }

        [Fact]
        public async Task TestBoradcast()
        {
            var packerId = "packer02";
            var client = _serviceProvider.GetRequiredService<RpcClient>();
            var pong = await client.ExecuteBoradcastAsync<string>("Ping", Encoding.UTF8.GetBytes("Ping"), "application/bson", MqttQualityOfServiceLevel.AtLeastOnce, TimeSpan.FromMinutes(1),
                2);
            Xunit.Assert.Equal("Pong", pong.ToString());
        }
    }
}
