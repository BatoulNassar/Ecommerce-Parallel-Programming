//using System;
//using System.Threading;
//using System.Threading.Tasks;

//namespace Ecommerce.Application.Interfaces
//{
//    public interface IBackgroundTaskQueue
//    {
//        void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem);

//        Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
//    }
//}
//using System;
//using System.Threading;
//using System.Threading.Tasks;

//namespace Ecommerce.Application.Interfaces
//{
//    public interface IBackgroundTaskQueue
//    {
//        void QueueBackgroundWorkItem(Func<IServiceProvider, CancellationToken, Task> workItem);

//        //Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
//    }
//}


using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ecommerce.Application.Interfaces
{
    public interface IBackgroundTaskQueue
    {
        void QueueBackgroundWorkItem(Func<IServiceProvider, CancellationToken, Task> workItem);

        Task<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(CancellationToken token);
    }
}