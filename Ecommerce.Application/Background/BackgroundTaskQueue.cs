using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Ecommerce.Application.Interfaces;

namespace Ecommerce.Application.Background
{
	public class BackgroundTaskQueue : IBackgroundTaskQueue
	{
		private readonly ConcurrentQueue<Func<IServiceProvider, CancellationToken, Task>> _tasks = new();
		private readonly SemaphoreSlim _signal = new(0);

		public void QueueBackgroundWorkItem(Func<IServiceProvider, CancellationToken, Task> workItem)
		{
			_tasks.Enqueue(workItem);
			_signal.Release();
		}

		public async Task<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(CancellationToken token)
		{
			await _signal.WaitAsync(token);
			_tasks.TryDequeue(out var task);
			return task!;
		}
	}
}