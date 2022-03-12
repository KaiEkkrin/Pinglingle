using System.Collections.Concurrent;
using System.Net.NetworkInformation;
using System.Reactive.Linq;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Pinglingle.Server.Hubs;
using Pinglingle.Shared.Model;

namespace Pinglingle.Server.Services;

public sealed class PingService : IHostedService
{
    private static readonly Action<ILogger, long, string, Exception?> AddedTargetMessage =
        LoggerMessage.Define<long, string>(
            LogLevel.Information,
            new EventId(2, nameof(AddedTargetMessage)),
            "Added target {Id} -> {Address}");

    private static readonly Action<ILogger, long, string, Exception?> DeletedTargetMessage =
        LoggerMessage.Define<long, string>(
            LogLevel.Information,
            new EventId(6, nameof(DeletedTargetMessage)),
            "Deleted target {Id} -> {Address}");

    private static readonly Action<ILogger, string, Exception?> FailedToPingMessage =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(3, nameof(FailedToPingMessage)),
            "Failed to ping {Address}");

    private static readonly Action<ILogger, string, IPStatus, Exception?> FailedToSaveSampleMessage =
        LoggerMessage.Define<string, IPStatus>(
            LogLevel.Error,
            new EventId(4, nameof(FailedToSaveSampleMessage)),
            "Failed to save ping of {Address} (Status = {Status})");

    private const int MaxSimultaneousPingsPerTarget = 5;

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PingService> _logger;

    private readonly Dictionary<long, Target> _targets = new();
    private readonly object _targetsLock = new();
    private readonly ConcurrentDictionary<string, int> _countByTarget = new();
    private IDisposable? _subscription;

    public PingService(
        IServiceProvider serviceProvider,
        ILogger<PingService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Fetch all the starting targets.
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<MyContext>();
        var targetsList = await context.Targets!.AsNoTracking().ToListAsync(cancellationToken);
        lock (_targetsLock)
        {
            foreach (var target in targetsList)
            {
                _targets[target.Id] = target;
                AddedTargetMessage(_logger, target.Id, target.Address, null);
            }
        }

        // Watch for target events
        Controllers.TargetsController.TargetAdded += OnTargetAdded;
        Controllers.TargetsController.TargetDeleted += OnTargetDeleted;

        // Ping on intervals, starting now.
        _subscription = Observable.Interval(TimeSpan.FromSeconds(1))
            .SelectMany(c => Observable.FromAsync(() => PingTargetsAsync(c)))
            .Subscribe(_ => { });
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _subscription?.Dispose();
        _subscription = null;
        Controllers.TargetsController.TargetDeleted -= OnTargetDeleted;
        Controllers.TargetsController.TargetAdded -= OnTargetAdded;
        return Task.CompletedTask;
    }

    private async Task PingTargetsAsync(long _)
    {
        // Kick off a ping of each target in turn with short delays between them.
        // Each ping will run as a task that we start and let go of so that nothing
        // is stuck waiting for it.
        await using var scope = _serviceProvider.CreateAsyncScope();
        MyContext? context = null; // only get if required

        var targets = GetTargets();
        foreach (var target in targets)
        {
            // Don't keep pinging a target that already has too many of them outstanding:
            var pingsInFlight = _countByTarget.AddOrUpdate(
                target.Address, _ => 1, (_, f) => f + 1);
            
            if (pingsInFlight <= MaxSimultaneousPingsPerTarget)
            {
                Task.Run(() => PingTargetAsync(target));
            }
            else
            {
                context ??= scope.ServiceProvider.GetRequiredService<MyContext>();
                await CompleteSampleAsync(target, new Sample
                {
                    TargetId = target.Id,
                    Date = DateTimeOffset.UtcNow,
                    Status = IPStatus.Unknown
                }, context, scope.ServiceProvider);
            }

            await Task.Delay(10);
        }
    }

    private IEnumerable<Target> GetTargets()
    {
        List<Target> targets = new(_targets.Count);
        lock (_targetsLock)
        {
            targets.AddRange(_targets.Values);
        }

        return targets;
    }

    private async Task PingTargetAsync(Target target)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<MyContext>();
        try
        {
            var (status, time) = await AddressUtility.MeasurePingMillisAsync(
                target.Address);
            await CompleteSampleAsync(target, new Sample
            {
                TargetId = target.Id,
                Date = DateTimeOffset.UtcNow,
                ResponseTimeMillis = time,
                Status = status
            }, context, scope.ServiceProvider);
        }
        catch (Exception e)
        {
            FailedToPingMessage(_logger, target.Address, e);
            await CompleteSampleAsync(target, new Sample
            {
                TargetId = target.Id,
                Date = DateTimeOffset.UtcNow,
                Status = IPStatus.Unknown
            }, context, scope.ServiceProvider);
        }
    }

    private async Task CompleteSampleAsync(
        Target target, Sample sample, MyContext context, IServiceProvider serviceProvider)
    {
        try
        {
            context.Samples!.Add(sample);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            // Send that sample to SignalR
            sample.Target = target;
            var hubContext = serviceProvider.GetRequiredService<IHubContext<PingHub>>();
            await hubContext.Clients.All.SendAsync("Sample", sample);
        }
        catch (Exception e)
        {
            FailedToSaveSampleMessage(_logger, target.Address, sample.Status, e);
        }
        finally
        {
            // Always make sure we decrement the number of outstanding pings
            // for this address
            _countByTarget.AddOrUpdate(target.Address, _ => 0, (_, f) => f - 1);
        }
    }

    private void OnTargetAdded(object? _, TargetEventArgs? eventArgs)
    {
        if (eventArgs?.Target is not { } target) return;
        lock (_targetsLock)
        {
            _targets[target.Id] = target;
        }

        AddedTargetMessage(_logger, target.Id, target.Address, null);
    }

    private void OnTargetDeleted(object? _, TargetEventArgs? eventArgs)
    {
        if (eventArgs?.Target is not { } target) return;
        lock (_targetsLock)
        {
            _targets.Remove(target.Id);
        }

        DeletedTargetMessage(_logger, target.Id, target.Address, null);
    }
}