using ConcertApi.Data;
using ConcertApi.Dtos;
using ConcertApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace ConcertApi.Controllers;
using ConcertApi.Dtos;


[ApiController]
[Route("api/v1/concerts")]
public class ConcertsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ConcertsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<PagedResponse<object>>> GetConcerts(
    string? search = null,
    string? city = null,
    int page = 1,
    int pageSize = 10)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var q = _db.Concerts.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(c => c.Title.Contains(search) || c.Artist.Contains(search) || c.Venue.Contains(search));

        if (!string.IsNullOrWhiteSpace(city))
            q = q.Where(c => c.City.Contains(city));

        var total = await q.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));

        // Keep page within range
        if (page > totalPages) page = totalPages;

        var rawItems = await q
            .OrderBy(c => c.ConcertDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.Artist,
                c.City,
                c.ConcertDate
            })
            .ToListAsync();

        var items = rawItems
            .Select(c => (object)new
            {
                c.Id,
                c.Title,
                c.Artist,
                c.City,
                c.ConcertDate,
                _links = new Dictionary<string, LinkDto>
                {
                    ["self"] = new($"/api/v1/concerts/{c.Id}"),
                    ["tickets"] = new($"/api/v1/tickets?concertId={c.Id}&page=1&pageSize=10")
                }
            })
            .ToList();

        string BuildUrl(int p) =>
            $"/api/v1/concerts?page={p}&pageSize={pageSize}"
            + (string.IsNullOrWhiteSpace(search) ? "" : $"&search={Uri.EscapeDataString(search)}")
            + (string.IsNullOrWhiteSpace(city) ? "" : $"&city={Uri.EscapeDataString(city)}");

        return Ok(new PagedResponse<object>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = total,
            TotalPages = totalPages,
            _links = new Dictionary<string, LinkDto>
            {
                ["self"] = new(BuildUrl(page)),
                ["next"] = new(page < totalPages ? BuildUrl(page + 1) : null),
                ["prev"] = new(page > 1 ? BuildUrl(page - 1) : null),
            }
        });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetConcert(long id)
    {
        var c = await _db.Concerts.AsNoTracking().FirstOrDefaultAsync(x => (long)x.Id == id);
        if (c is null) return NotFound();

        return Ok(new
        {
            c.Id,
            c.Title,
            c.Artist,
            c.Venue,
            c.City,
            c.ConcertDate,
            c.Description,
            _links = new Dictionary<string, LinkDto>
            {
                ["self"] = new($"/api/v1/concerts/{c.Id}"),
                ["tickets"] = new($"/api/v1/tickets?concertId={c.Id}&page=1&pageSize=10"),
                ["update"] = new($"/api/v1/concerts/{c.Id}", "PUT"),
                ["delete"] = new($"/api/v1/concerts/{c.Id}", "DELETE")
            }
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateConcert(long id, [FromBody] UpdateConcertRequest req)
    {
        // Business rule (optional but good)
        if (req.ConcertDate <= DateTime.UtcNow.AddMinutes(-1))
            return BadRequest(new { message = "ConcertDate must be in the future." });

        var c = await _db.Concerts.FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();

        c.Title = req.Title.Trim();
        c.Artist = req.Artist.Trim();
        c.Venue = req.Venue.Trim();
        c.City = req.City.Trim();
        c.ConcertDate = req.ConcertDate;
        c.Description = req.Description;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteConcert(long id)
    {
        var c = await _db.Concerts.FirstOrDefaultAsync(x => (long)x.Id == id);
        if (c is null) return NotFound();

        _db.Concerts.Remove(c);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateConcert([FromBody] CreateConcertRequest req)
    {
        // Business rule: date must be in the future (optional but good)
        if (req.ConcertDate <= DateTime.UtcNow.AddMinutes(-1))
            return BadRequest(new { message = "ConcertDate must be in the future." });

        var concert = new Concert
        {
            Title = req.Title.Trim(),
            Artist = req.Artist.Trim(),
            Venue = req.Venue.Trim(),
            City = req.City.Trim(),
            ConcertDate = req.ConcertDate,
            Description = req.Description,
            CreatedAt = DateTime.UtcNow
        };

        _db.Concerts.Add(concert);
        await _db.SaveChangesAsync();

        return Created($"/api/v1/concerts/{concert.Id}", new
        {
            concert.Id,
            concert.Title,
            concert.Artist,
            concert.Venue,
            concert.City,
            concert.ConcertDate,
            concert.Description,
            concert.CreatedAt
            // keep your _links if you already return them
        });
    }
}