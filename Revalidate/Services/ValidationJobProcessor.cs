using Revalidate.Entities;
using System.Threading.Channels;

namespace Revalidate.Services;

public sealed class ValidationJobProcessor : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;

    private readonly Channel<Guid> channel;

    public ValidationJobProcessor(IServiceScopeFactory scopeFactory)
    {
        this.scopeFactory = scopeFactory;

        var options = new BoundedChannelOptions(10)
        {
            FullMode = BoundedChannelFullMode.Wait
        };

        channel = Channel.CreateBounded<Guid>(options);
    }

    public async ValueTask EnqueueAsync(Guid validationRequestId, CancellationToken cancellationToken)
    {
        await channel.Writer.WriteAsync(validationRequestId, cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var validationService = scope.ServiceProvider.GetRequiredService<IValidationService>();

            //await validationService.GetAllInProgressValidationRequestsAsync(stoppingToken);
        }

        await foreach (var requestId in channel.Reader.ReadAllAsync(stoppingToken))
        {
            await using var scope = scopeFactory.CreateAsyncScope();

            var validationService = scope.ServiceProvider.GetRequiredService<IValidationService>();

            var validationRequest = await validationService.GetValidationRequestByIdAsync(requestId, stoppingToken);
        }
    }
}
