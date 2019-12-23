using System;
using System.Threading.Tasks;

namespace Abp.Mqtt.Rpc
{
    public interface IRpcServer
    {
        void Start();

        Task Stop(TimeSpan timeout = default);

        void ForceStop();

        void Wait();
    }
}
