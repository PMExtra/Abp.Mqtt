using System;

namespace Abp.Mqtt.Rpc
{
    public class RpcException : Exception
    {
        public RpcException(string message, int code = 0) : base(message)
        {
            HResult = code;
        }

        public RpcException(string message, Exception innerException, int code = 0) : base(message, innerException)
        {
            HResult = code;
        }
    }
}
