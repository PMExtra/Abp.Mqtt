using Abp.Mqtt.Rpc;
using System;
using System.Collections.Generic;
using System.Text;

namespace Abp.Mqtt.Packer
{
    public class RpcService:IRpcService
    {
        private readonly IServiceProvider _serviceProvider;

        public RpcService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public string Ping(string ping)
        {
            if (ping == "Ping") return "Pong";
            throw new ArgumentException("Unexpected argument.");
        }
    }
}
