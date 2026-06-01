namespace ConcertApi.Models;

public class Ticket
{
    public long Id { get; set; }

    public long ConcertId { get; set; }
    public Concert? Concert { get; set; }

    public decimal Price { get; set; }
    public string SeatNumber { get; set; } = "";
    public string Status { get; set; } = "Available";
    public DateTime CreatedAt { get; set; }
}