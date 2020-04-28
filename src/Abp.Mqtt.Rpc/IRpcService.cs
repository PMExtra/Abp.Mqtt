namespace Abp.Mqtt.Rpc
{
    public interface IRpcService
    {
        public RpcContext CurrentContext { get; set; }
    }
}
