using System.Threading;
using System.Threading.Tasks;

namespace Abp.Mqtt.Rpc.Internal
{
    internal class CancellableTask
    {
        public CancellableTask(Task task, CancellationTokenSource cancellationTokenSource)
        {
            Task = task;
            CancellationTokenSource = cancellationTokenSource;
        }

        public Task Task { get; }

        public CancellationTokenSource CancellationTokenSource { get; }
    }
}
