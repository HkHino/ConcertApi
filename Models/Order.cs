namespace ConcertApi.Models;

public class Order
{
    public long Id { get; set; }

    public long UserId { get; set; }
    public User? User { get; set; }

    public long TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    public string Status { get; set; } = "Active";
    public DateTime PurchasedAt { get; set; }
}