using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Abp.Mqtt.Rpc.Test
{
    public class RpcService : IRpcService
    {
        private readonly IServiceProvider _serviceProvider;

        public RpcService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<string> Ping(string ping)
        {
            var beginDate = DateTime.Now;

            //Console.WriteLine("entry -> "+DateTime.Now.ToString("HH:mm:ss.fff"));
            //await Task.Delay(1000);
            //Console.WriteLine("Task.Delay complete -> " + DateTime.Now.ToString("HH:mm:ss.fff"));

            var endDate = DateTime.Now;
            Console.WriteLine("id=" + ping + " " + beginDate.ToString("HH:mm:ss.fff") + " -> " + endDate.ToString(endDate.ToString("HH:mm:ss.fff")));
            return "Pong";
            throw new ArgumentException("Unexpected argument.");
        }
    }
}
