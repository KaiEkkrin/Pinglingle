using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pinglingle.Shared.Model;

namespace Pinglingle.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class DigestsController : ControllerBase
{
    private readonly MyContext _context;

    public DigestsController(MyContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get(long? oldest = null, long? newest = null)
    {
        // There must always be an `oldest`
        if (!oldest.HasValue) return BadRequest();

        var o = DateTimeOffset.FromUnixTimeSeconds(oldest.Value);
        if (newest.HasValue)
        {
            var n = DateTimeOffset.FromUnixTimeSeconds(newest.Value);
            return Ok(await EnumerateDigestsAsync(o, n));
        }

        return Ok(await EnumerateDigestsAsync(o));
    }

    private Task<List<Digest>> EnumerateDigestsAsync(
        DateTimeOffset oldest, DateTimeOffset newest)
    {
        return _context.Digests!
            .AsNoTracking()
            .Where(s => s.StartTime >= oldest && s.StartTime < newest)
            .OrderBy(s => s.StartTime)
            .ToListAsync();
    }

    private Task<List<Digest>> EnumerateDigestsAsync(DateTimeOffset oldest)
    {
        return _context.Digests!
            .AsNoTracking()
            .Where(s => s.StartTime >= oldest)
            .OrderBy(s => s.StartTime)
            .ToListAsync();
    }
}