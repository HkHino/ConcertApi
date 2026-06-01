namespace ConcertApi.Models;

public class Concert
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string Artist { get; set; } = "";
    public string Venue { get; set; } = "";
    public string City { get; set; } = "";
    public DateTime ConcertDate { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<Ticket> Tickets { get; set; } = new();
}