using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Abp.Mqtt.Contexts;
using MQTTnet.Protocol;

namespace Abp.Mqtt.Rpc.Test.Mocking
{
    public class RpcClient : RpcClientBase
    {
        public const string ServerId = "Cloud.Rpc";

        public RpcClient(ManagedMqttContext context) : base(context)
        {
        }

        public async Task<int> Ping(int timeout = 1)
        {
            var watch = new Stopwatch();
            watch.Start();
            var pong = await ExecuteAsync<string>(ServerId, "Ping", "Ping", MqttQualityOfServiceLevel.AtLeastOnce, TimeSpan.FromSeconds(timeout));
            watch.Stop();
            if (pong != "Pong") throw new Exception("Unexpected response.");
            return Convert.ToInt32(watch.ElapsedMilliseconds);
        }

        public async Task Sleep(int seconds)
        {
            await ExecuteAsync(ServerId, "Sleep", null, MqttQualityOfServiceLevel.ExactlyOnce, TimeSpan.FromSeconds(seconds + 10));
        }

        public async Task<string> WhoAmI()
        {
            return await ExecuteAsync<string>(ServerId, "WhoAmI", null, MqttQualityOfServiceLevel.AtLeastOnce, TimeSpan.FromSeconds(10));
        }

        public async Task Fire()
        {
            await FireAndForgetAsync(ServerId, "Fire", null, MqttQualityOfServiceLevel.AtLeastOnce);
        }

        public async Task<string> GetDefaultSerializer()
        {
            return await ExecuteAsync<string>(ServerId, "GetDefaultSerializer", null, MqttQualityOfServiceLevel.AtLeastOnce, TimeSpan.FromSeconds(10));
        }
    }
}
