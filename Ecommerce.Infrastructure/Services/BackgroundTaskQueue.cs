using Ecommerce.Application.Interfaces;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Ecommerce.Infrastructure.Services
{
    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private readonly Channel<Func<IServiceProvider, CancellationToken, Task>> _queue;

        public BackgroundTaskQueue()
        {
            var options = new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait
            };

            _queue = Channel.CreateBounded<Func<IServiceProvider, CancellationToken, Task>>(options);
        }

        public void QueueBackgroundWorkItem(
            Func<IServiceProvider, CancellationToken, Task> workItem)
        {
            if (workItem == null)
                throw new ArgumentNullException(nameof(workItem));

            _queue.Writer.TryWrite(workItem);
        }

        public async Task<Func<IServiceProvider, CancellationToken, Task>>
            DequeueAsync(CancellationToken cancellationToken)
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}