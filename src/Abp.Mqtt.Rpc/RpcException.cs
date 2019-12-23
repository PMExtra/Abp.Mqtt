using System;

namespace Abp.Mqtt.Rpc
{
    public class RpcException : Exception
    {
        public RpcException(string message) : base(message)
        {
        }

        public RpcException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
