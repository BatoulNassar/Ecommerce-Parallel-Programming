using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Ecommerce.Application.Interfaces;

public class QueuedHostedService : BackgroundService
{
    private readonly IBackgroundTaskQueue _queue;
    private readonly IServiceProvider _provider;

    public QueuedHostedService(IBackgroundTaskQueue queue, IServiceProvider provider)
    {
        _queue = queue;
        _provider = provider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var workItem = await _queue.DequeueAsync(stoppingToken);

            using var scope = _provider.CreateScope();

            await workItem(scope.ServiceProvider, stoppingToken);
        }
    }
}