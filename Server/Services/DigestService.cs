using System.Reactive.Linq;
using Microsoft.EntityFrameworkCore;
using Pinglingle.Shared.Model;

namespace Pinglingle.Server.Services;

public sealed class DigestService : IHostedService
{
    private static readonly Action<ILogger, int, int, Exception?> DeletedOldMessage =
        LoggerMessage.Define<int, int>(
            LogLevel.Information,
            new EventId(1, nameof(DigestService)),
            "Deleted {DigestsDeleted} old digests and {SamplesDeleted} old samples");

    private static readonly Action<ILogger, string, Exception?> FailedToCreateDigestMessage =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(2, nameof(DigestService)),
            "Error creating digest (will retry) : {Message}");

    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DigestService> _logger;

    private IDisposable? _subscription;

    // We'll delete samples and digests older than this to keep the database clean
    private static DateTimeOffset MinSampleAge => DateTimeOffset.UtcNow.Date.AddDays(-7);

    public DigestService(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<DigestService> logger)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Only issue SQL to delete old things if we have a SQL database!
        var deleteOld = _configuration["ConnectionString"] is { Length: > 0 };

        _subscription = Observable.Interval(Digest.Period)
            .Prepend(-1L)
            .SelectMany(c => Observable.FromAsync(() => CreateDigestsAsync(c, deleteOld)))
            .Subscribe(_ => { });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _subscription?.Dispose();
        _subscription = null;
        return Task.CompletedTask;
    }

    private async Task CreateDigestsAsync(long counter, bool deleteOld)
    {
        try
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MyContext>();

            if (deleteOld)
            {
                await DeleteOldAsync(context);
            }
        }
        catch (Exception e)
        {
            // This will just get retried, it should be fine eventually
            FailedToCreateDigestMessage(_logger, e.Message, e);
        }
    }

    private async Task DeleteOldAsync(MyContext context)
    {
        // Delete too-old digests and samples
        var minSampleAge = MinSampleAge;
        var minDigestAge = minSampleAge - Digest.Period * 2;
        var digestsDeleted = await context.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM \"Digests\" WHERE \"StartTime\" < {minDigestAge}");

        var samplesDeleted = await context.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM \"Samples\" WHERE \"Date\" < {minSampleAge}");

        DeletedOldMessage(_logger, digestsDeleted, samplesDeleted, null);
    }
}