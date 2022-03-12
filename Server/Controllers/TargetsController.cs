using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    private readonly MyContext _context;
    private readonly ILogger<TargetsController> _logger;

    public TargetsController(
        MyContext context,
        ILogger<TargetsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public IAsyncEnumerable<Target> GetAsync()
    {
        return _context.Targets!.AsNoTracking().AsAsyncEnumerable();
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
        _context.Targets!.Remove(target);
        await _context.SaveChangesAsync();
        return true;
    }
}