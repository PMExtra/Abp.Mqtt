using System;

namespace Abp.Mqtt.Rpc
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RpcMethodAttribute : Attribute
    {
        public RpcMethodAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
