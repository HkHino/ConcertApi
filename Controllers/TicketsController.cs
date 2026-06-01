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
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetTicket(long id)
    {
        var t = await _db.Tickets.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (t is null) return NotFound(new { message = "Ticket not found." });

        return Ok(new
        {
            t.Id,
            t.ConcertId,
            t.Price,
            t.SeatNumber,
            t.Status,
            _links = BuildTicketLinks(t.Id, t.ConcertId, t.Status)
        });
    }
}