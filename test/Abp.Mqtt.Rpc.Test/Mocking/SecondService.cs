namespace Abp.Mqtt.Rpc.Test.Mocking
{
    public class SecondService : IRpcService
    {
        public bool Fired { get; private set; }

        public RpcContext CurrentContext { get; set; }

        public void Fire()
        {
            Fired = true;
        }
    }
}
