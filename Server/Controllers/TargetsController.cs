using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Pinglingle.Server.Hubs;
using Pinglingle.Shared;
using Pinglingle.Shared.Model;

namespace Pinglingle.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class TargetsController : ControllerBase
{
    private static readonly Action<ILogger, string, string, Exception?> FailedToAddTargetMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(1, nameof(FailedToAddTargetMessage)),
            "Failed to add target. Address = {Address}. Message = {Message}");

    private static readonly Action<ILogger, long, string, Exception?> FailedToDeleteTargetMessage =
        LoggerMessage.Define<long, string>(
            LogLevel.Error,
            new EventId(5, nameof(FailedToDeleteTargetMessage)),
            "Failed to delete target. Id = {Id}. Message = {Message}");

    private readonly MyContext _context;
    private readonly IHubContext<PingHub> _hubContext;
    private readonly ILogger<TargetsController> _logger;

    private static event EventHandler<TargetEventArgs>? _targetAdded;
    private static event EventHandler<TargetEventArgs>? _targetDeleted;

    public TargetsController(
        MyContext context,
        IHubContext<PingHub> hubContext,
        ILogger<TargetsController> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
    }

    // Encapsulation? What encapsulation?
    internal static event EventHandler<TargetEventArgs> TargetAdded
    {
        add => _targetAdded += value;
        remove => _targetAdded -= value;
    }

    internal static event EventHandler<TargetEventArgs> TargetDeleted
    {
        add => _targetDeleted += value;
        remove => _targetDeleted -= value;
    }

    [HttpGet]
    public IAsyncEnumerable<Target> GetAsync()
    {
        return _context.Targets!.AsNoTracking()
            .OrderBy(t => t.Address)
            .AsAsyncEnumerable();
    }

    [HttpPost]
    public async Task<AddTargetResult> AddTargetAsync(Target target)
    {
        try
        {
            // Verify the target address is sane
            var address = await AddressUtility.ResolveAddressAsync(target.Address);
            if (address is null) return new AddTargetResult(null, "Failed to lookup address.");

            _context.Targets!.Add(target);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            await _hubContext.Clients.All.SendAsync("Target", target);
            _targetAdded?.Invoke(this, new TargetEventArgs(target));
            return new AddTargetResult(target.Id);
        }
        catch (Exception e)
        {
            FailedToAddTargetMessage(_logger, target.Address, e.Message, e);
            return new AddTargetResult(null, e.Message);
        }
    }

    [HttpDelete]
    public async Task<bool> DeleteTargetAsync(long targetId)
    {
        var target = await _context.Targets!.Where(t => t.Id == targetId)
            .SingleOrDefaultAsync();

        if (target is null) return false;
        try
        {
            _context.Targets!.Remove(target);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            await _hubContext.Clients.All.SendAsync("TargetDeleted", target);
            _targetDeleted?.Invoke(this, new TargetEventArgs(target));
            return true;
        }
        catch (Exception e)
        {
            FailedToDeleteTargetMessage(_logger, targetId, e.Message, e);
            return false;
        }
    }
}