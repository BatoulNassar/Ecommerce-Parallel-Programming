using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Infrastructure.Services;

public class SalesBatchHostedService : BackgroundService
{
    private readonly SalesBatchProcessingService _batchService;

    public SalesBatchHostedService(SalesBatchProcessingService batchService)
    {
        _batchService = batchService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Batch Service Started");

        await _batchService.ProcessAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);

            await _batchService.ProcessAsync(stoppingToken);
        }
    }
}