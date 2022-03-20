using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    public async Task<IActionResult> Get(
        long? oldest = null, long? newest = null, int? count = null)
    {
        // Some validation
        switch ((oldest, newest, count))
        {
            case (null, null, null):
                return BadRequest();

            case ({ } ov, { } nv, _) when nv < ov:
                return BadRequest();

            case (_, _, > 10000):
                return BadRequest();
            
            default:
                break;
        }

        // Construct the query
        var query = _context.Digests!.AsNoTracking();

        // Apply the date filter(s)
        var o = oldest.HasValue
            ? (DateTimeOffset?)DateTimeOffset.FromUnixTimeSeconds(oldest.Value)
            : null;

        var n = newest.HasValue
            ? (DateTimeOffset?)DateTimeOffset.FromUnixTimeSeconds(newest.Value)
            : null;

        query = (o, n) switch
        {
            ({ } a, { } b) => query.Where(s => s.StartTime >= a && s.StartTime < b),
            ({ } a, null) => query.Where(s => s.StartTime >= a),
            (null, { } b) => query.Where(s => s.StartTime < b),
            _ => query
        };

        // Order, and apply the count filter
        query = query.OrderBy(s => s.StartTime);
        if (count is { } c)
        {
            query = query.Take(c);
        }

        return Ok(await query.ToListAsync());
    }
}