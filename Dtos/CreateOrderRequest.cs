using System.ComponentModel.DataAnnotations;

namespace ConcertApi.Dtos;

public class CreateOrderRequest
{
    [Required]
    [Range(1, long.MaxValue)]
    public long TicketId { get; set; }
}