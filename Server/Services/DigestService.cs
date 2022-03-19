using System.Net.NetworkInformation;
using System.Reactive.Linq;
using Microsoft.EntityFrameworkCore;
using Pinglingle.Shared;
using Pinglingle.Shared.Model;

namespace Pinglingle.Server.Services;

public sealed class DigestService : IHostedService
{
    private static readonly Action<ILogger, int, int, Exception?> DeletedOldMessage =
        LoggerMessage.Define<int, int>(
            LogLevel.Information,
            new EventId(1, nameof(DigestService)),
            "Deleted {DigestsDeleted} old digests and {SamplesDeleted} old samples");

    private static readonly Action<ILogger, int, DateTimeOffset, Exception?>
        DigestedSamplesMessage =
            LoggerMessage.Define<int, DateTimeOffset>(
                LogLevel.Information,
                new EventId(2, nameof(DigestService)),
                "Digested {TotalCount} samples in slot {StartTime}");

    private static readonly Action<ILogger, string, Exception?> FailedToCreateDigestMessage =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(3, nameof(DigestService)),
            "Error creating digest (will retry) : {Message}");

    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DigestService> _logger;

    private int _simultaneousRuns;
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

        _subscription = Observable.Interval(Digest.Period / 5)
            .Prepend(-1L)
            .SelectMany(c => Observable.FromAsync(() => RunTaskAsync(c, deleteOld)))
            .Subscribe(_ => { });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _subscription?.Dispose();
        _subscription = null;
        return Task.CompletedTask;
    }

    private async Task RunTaskAsync(long counter, bool deleteOld)
    {
        try
        {
            // Sanity check -- shouldn't happen but I want to be sure :P
            var simultaneousRuns = Interlocked.Increment(ref _simultaneousRuns);
            if (simultaneousRuns > 1)
            {
                throw new InvalidOperationException(
                    $"Detected {simultaneousRuns} simultaneous runs");
            }

            await using var scope = _serviceProvider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MyContext>();

            if (deleteOld)
            {
                await DeleteOldAsync(context);
            }

            await CreateDigestsAsync(context);
        }
        catch (Exception e)
        {
            // This will just get retried, it should be fine eventually
            FailedToCreateDigestMessage(_logger, e.Message, e);
        }
        finally
        {
            Interlocked.Decrement(ref _simultaneousRuns);
        }
    }

    private async Task CreateDigestsAsync(MyContext context)
    {
        // Accumulate response times and error counts here by target
        Dictionary<long, DigestState> statesByTarget = new();

        // Loop over creating digests for time slots until we don't have a
        // full time slot to digest anymore
        while (true)
        {
            var createdDigest = await CreateTimeSlotDigestsAsync(context, statesByTarget);
            if (!createdDigest) break;

            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
        }
    }

    private async Task<bool> CreateTimeSlotDigestsAsync(
        MyContext context, Dictionary<long, DigestState> statesByTarget)
    {
        // We need to separate creating each time slot into different calls because
        // we can't save changes while enumerating -- but we don't want to end up
        // with one enormous change list at the end that might cause an exception
        // to be thrown
        DateTimeOffset? timeSlot = null;

        // Read through our undigested samples, oldest first.
        var needsSave = false;
        await foreach (var sample in context.Samples!
                .Where(s => (!s.IsDigested) && s.TargetId != null)
                .OrderBy(s => s.Date)
                .AsAsyncEnumerable())
        {
            // Check this sample's five minute floor. If it's different from our
            // current time slot we have a new one
            var sampleFloor = MathUtil.FiveMinuteFloor(sample.Date);
            if (timeSlot is { } startTime && sampleFloor > startTime)
            {
                // Write out the digests for this time slot and clear the state
                // for the next one.
                var totalCount = 0;
                foreach (var (targetId, targetState) in statesByTarget)
                {
                    var digest = targetState.CreateDigest(targetId, startTime);
                    context.Digests!.Add(digest);
                    targetState.Clear();
                    totalCount += digest.SampleCount;
                }

                if (totalCount > 0)
                {
                    needsSave = true;
                }

                DigestedSamplesMessage(_logger, totalCount, startTime, null);
                break;
            }

            timeSlot = sampleFloor;

            // Count this sample in the current state
            if (!statesByTarget.TryGetValue(sample.TargetId!.Value, out var state))
            {
                state = new();
                statesByTarget.Add(sample.TargetId!.Value, state);
            }

            state.Add(sample);
        }

        return needsSave;
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

    private sealed class DigestState
    {
        public List<int> ResponseTimes { get; } = new();
        public int ErrorCount { get; set; }

        public void Add(Sample sample)
        {
            if (sample.ResponseTimeMillis.HasValue && sample.Status == IPStatus.Success)
            {
                ResponseTimes.Add(sample.ResponseTimeMillis.Value);
            }
            else
            {
                ++ErrorCount;
            }

            // After adding this sample flag it as digested -- this will be written
            // to the database when the digest is saved
            sample.IsDigested = true;
        }

        public void Clear()
        {
            ResponseTimes.Clear();
            ErrorCount = 0;
        }

        public Digest CreateDigest(long targetId, DateTimeOffset startTime) => new Digest
        {
            TargetId = targetId,
            StartTime = startTime,
            SampleCount = ResponseTimes.Count + ErrorCount,
            Percentile5 = MathUtil.Percentile(ResponseTimes, 5),
            Percentile50 = MathUtil.Percentile(ResponseTimes, 50),
            Percentile95 = MathUtil.Percentile(ResponseTimes, 95),
            ErrorCount = ErrorCount
        };
    }
}