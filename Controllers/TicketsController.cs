using ConcertApi.Data;
using ConcertApi.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConcertApi.Controllers;

[ApiController]
[Route("api/v1/tickets")]
public class TicketsController : ControllerBase
{
    private readonly AppDbContext _db;
    public TicketsController(AppDbContext db) => _db = db;

    // GET /api/v1/tickets?concertId=1&status=Available&page=1&pageSize=10
    [HttpGet]
    public async Task<ActionResult<PagedResponse<object>>> GetTickets(
     long? concertId = null,
     string? status = null,
     int page = 1,
     int pageSize = 10)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        // Validate status (optional but best practice)
        if (!string.IsNullOrWhiteSpace(status))
        {
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Available", "Reserved", "Sold"
        };

            if (!allowed.Contains(status))
                return BadRequest(new { message = "Invalid status. Use: Available, Reserved, Sold." });

            // normalize casing so filtering matches DB values
            status = allowed.First(s => s.Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        var q = _db.Tickets.AsNoTracking().AsQueryable();

        if (concertId.HasValue)
            q = q.Where(t => t.ConcertId == concertId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(t => t.Status == status);

        var total = await q.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        if (page > totalPages) page = totalPages;

        var raw = await q
            .OrderBy(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new { t.Id, t.ConcertId, t.Price, t.SeatNumber, t.Status })
            .ToListAsync();

        var items = raw.Select(t => (object)new
        {
            t.Id,
            t.ConcertId,
            t.Price,
            t.SeatNumber,
            t.Status,
            _links = BuildTicketLinks(t.Id, t.ConcertId, t.Status)
        }).ToList();

        string BuildUrl(int p) =>
            $"/api/v1/tickets?page={p}&pageSize={pageSize}"
            + (concertId.HasValue ? $"&concertId={concertId.Value}" : "")
            + (!string.IsNullOrWhiteSpace(status) ? $"&status={Uri.EscapeDataString(status)}" : "");

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

    // Helper: conditional HATEOAS links
    private Dictionary<string, LinkDto> BuildTicketLinks(long ticketId, long concertId, string status)
    {
        var links = new Dictionary<string, LinkDto>
        {
            ["self"] = new($"/api/v1/tickets/{ticketId}"),
            ["concert"] = new($"/api/v1/concerts/{concertId}")
        };

        // Only offer "buy" if the ticket is available
        if (status == "Available")
            links["buy"] = new($"/api/v1/orders", "POST");

        return links;
    }

    // GET /api/v1/tickets/5
    // GET /api/v1/tickets/5
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetTicket(long id)
    {
        var raw = await _db.Tickets.AsNoTracking()
            .Where(x => x.Id == id)
            .Join(_db.Concerts.AsNoTracking(),
                  ticket => ticket.ConcertId,
                  concert => concert.Id,
                  (ticket, concert) => new
                  {
                      ticket.Id,
                      ticket.ConcertId,
                      concertTitle = concert.Title,
                      concertArtist = concert.Artist,
                      concertCity = concert.City,
                      concertDate = concert.ConcertDate,
                      ticket.Price,
                      ticket.SeatNumber,
                      ticket.Status
                  })
            .FirstOrDefaultAsync();

        if (raw is null) return NotFound(new { message = "Ticket not found." });

        var result = new
        {
            raw.Id,
            raw.ConcertId,
            raw.concertTitle,
            raw.concertArtist,
            raw.concertCity,
            raw.concertDate,
            raw.Price,
            raw.SeatNumber,
            raw.Status,
            _links = BuildTicketLinks(raw.Id, raw.ConcertId, raw.Status)
        };

        return Ok(result);
    }

    [HttpGet("search")]
    public async Task<ActionResult<PagedResponse<object>>> SearchTickets(
    string? artist = null,
    string? concertTitle = null,
    string? city = null,
    int page = 1,
    int pageSize = 10)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var q = from t in _db.Tickets.AsNoTracking()
                join c in _db.Concerts.AsNoTracking() on t.ConcertId equals c.Id
                select new { t, c };

        if (!string.IsNullOrWhiteSpace(artist))
        {
            var a = artist.Trim().ToLower();
            q = q.Where(x => x.c.Artist.ToLower().Contains(a));
        }

        if (!string.IsNullOrWhiteSpace(concertTitle))
        {
            var ct = concertTitle.Trim().ToLower();
            q = q.Where(x => x.c.Title.ToLower().Contains(ct));
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var cy = city.Trim().ToLower();
            q = q.Where(x => x.c.City.ToLower().Contains(cy));
        }

        var total = await q.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        if (page > totalPages) page = totalPages;

        var raw = await q
    .OrderBy(x => x.c.ConcertDate)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .Select(x => new
    {
        x.t.Id,
        x.t.ConcertId,
        concertTitle = x.c.Title,
        concertArtist = x.c.Artist,
        concertCity = x.c.City,
        concertDate = x.c.ConcertDate,
        x.t.Price,
        x.t.SeatNumber,
        x.t.Status
    })
    .ToListAsync();

        // Build links AFTER query executes (Option A)
        var items = raw.Select(x => (object)new
        {
            x.Id,
            x.ConcertId,
            x.concertTitle,
            x.concertArtist,
            x.concertCity,
            x.concertDate,
            x.Price,
            x.SeatNumber,
            x.Status,
            _links = BuildTicketLinks(x.Id, x.ConcertId, x.Status)
        }).ToList();

        string BuildUrl(int p) =>
            $"/api/v1/tickets/search?page={p}&pageSize={pageSize}"
            + (!string.IsNullOrWhiteSpace(artist) ? $"&artist={Uri.EscapeDataString(artist)}" : "")
            + (!string.IsNullOrWhiteSpace(concertTitle) ? $"&concertTitle={Uri.EscapeDataString(concertTitle)}" : "")
            + (!string.IsNullOrWhiteSpace(city) ? $"&city={Uri.EscapeDataString(city)}" : "");

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
}