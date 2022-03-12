using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pinglingle.Shared.Model;

namespace Pinglingle.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class SamplesController : ControllerBase
{
    private readonly MyContext _context;

    public SamplesController(MyContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get(long targetId, long? oldest = null, long? newest = null)
    {
        // There must always be an `oldest`
        if (!oldest.HasValue) return BadRequest();

        var o = DateTimeOffset.FromUnixTimeSeconds(oldest.Value);
        if (newest.HasValue)
        {
            var n = DateTimeOffset.FromUnixTimeSeconds(newest.Value);
            return Ok(await EnumerateSamplesAsync(targetId, o, n));
        }

        return Ok(await EnumerateSamplesAsync(targetId, o));
    }

    private Task<List<Sample>> EnumerateSamplesAsync(
        long targetId, DateTimeOffset oldest, DateTimeOffset newest)
    {
        return _context.Samples!
            .AsNoTracking()
            .Where(s => s.TargetId == targetId && s.Date > oldest && s.Date <= newest)
            .OrderBy(s => s.Date)
            .ToListAsync();
    }

    private Task<List<Sample>> EnumerateSamplesAsync(
        long targetId, DateTimeOffset oldest)
    {
        return _context.Samples!
            .AsNoTracking()
            .Where(s => s.TargetId == targetId && s.Date > oldest)
            .OrderBy(s => s.Date)
            .ToListAsync();
    }
}