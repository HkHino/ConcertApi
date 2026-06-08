using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ConcertApi.Data;
using ConcertApi.Dtos;
using ConcertApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConcertApi.Controllers;

[ApiController]
[Route("api/v1/orders")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    public OrdersController(AppDbContext db) => _db = db;

    public record CreateOrderRequest(long TicketId);

    // POST /api/v1/orders  { "ticketId": 15 }
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest req)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                       ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrWhiteSpace(userIdStr))
            return Unauthorized(new { message = "Missing user id claim." });

        var userId = long.Parse(userIdStr);

        // Get ticket and validate status
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == req.TicketId);
        if (ticket is null)
            return NotFound(new { message = "Ticket not found." });

        if (ticket.Status != "Available")
            return BadRequest(new { message = "Ticket is not available." });

        // Mark ticket sold + create order
        ticket.Status = "Sold";

        var order = new Order
        {
            UserId = userId,
            TicketId = ticket.Id,
            Status = "Active",
            PurchasedAt = DateTime.UtcNow
        };

        _db.Orders.Add(order);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Covers “double-buy” via unique constraint on orders.ticket_id
            return Conflict(new { message = "Ticket already purchased." });
        }

        return Created($"/api/v1/orders/{order.Id}", new
        {
            order.Id,
            order.UserId,
            order.TicketId,
            order.Status,
            order.PurchasedAt,
            ticketStatus = ticket.Status, // Show updated ticket status in response, and confirms that the purchase succeeded
            _links = new Dictionary<string, LinkDto>
            {
                ["self"] = new($"/api/v1/orders/{order.Id}"),
                ["ticket"] = new($"/api/v1/tickets/{order.TicketId}"),
                ["listMine"] = new($"/api/v1/orders?mine=true&page=1&pageSize=10")
            }
        });
    }

    // GET /api/v1/orders?mine=true&page=1&pageSize=10
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<object>>> GetOrders(
    bool mine = true,
    string? status = null,
    int page = 1,
    int pageSize = 10)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var q = _db.Orders.AsNoTracking().AsQueryable();

        // SECURITY: mine=false means "view all orders" -> Admin only
        if (mine)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrWhiteSpace(userIdStr))
                return Unauthorized(new { message = "Missing user id claim." });

            var userId = long.Parse(userIdStr);
            q = q.Where(o => o.UserId == userId);
        }
        else
        {
            // Optional: allow only Admin to list all orders
            if (!User.IsInRole("Admin"))
                return Forbid();
        }

        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(o => o.Status == status);

        var total = await q.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));

        // Keep page within range
        if (page > totalPages) page = totalPages;

        var raw = await q
            .OrderByDescending(o => o.PurchasedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new { o.Id, o.UserId, o.TicketId, o.Status, o.PurchasedAt })
            .ToListAsync();

        var items = raw.Select(o => (object)new
        {
            o.Id,
            o.UserId,
            o.TicketId,
            o.Status,
            o.PurchasedAt,
            _links = new Dictionary<string, LinkDto>
            {
                ["self"] = new($"/api/v1/orders/{o.Id}"),
                ["ticket"] = new($"/api/v1/tickets/{o.TicketId}")
            }
        }).ToList();

        string BuildUrl(int p) =>
            $"/api/v1/orders?mine={mine.ToString().ToLower()}&page={p}&pageSize={pageSize}"
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
                ["prev"] = new(page > 1 ? BuildUrl(page - 1) : null)
            }
        });
    }

    // GET /api/v1/orders/123 (only owner or Admin should see it)
    [Authorize]
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetOrder(long id)
    {
        var order = await _db.Orders.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
            return NotFound(new { message = "Order not found." });

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                       ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrWhiteSpace(userIdStr))
            return Unauthorized(new { message = "Missing user id claim." });

        var userId = long.Parse(userIdStr);
        var isAdmin = User.IsInRole("Admin");

        // SECURITY: Only order owner (or Admin) can access a specific order
        if (!isAdmin && order.UserId != userId)
            return Forbid(); // 403

        return Ok(new
        {
            order.Id,
            order.UserId,
            order.TicketId,
            order.Status,
            order.PurchasedAt,
            _links = new Dictionary<string, LinkDto>
            {
                ["self"] = new($"/api/v1/orders/{order.Id}"),
                ["ticket"] = new($"/api/v1/tickets/{order.TicketId}"),
                ["listMine"] = new($"/api/v1/orders?mine=true&page=1&pageSize=10")
            }
        });
    }
}