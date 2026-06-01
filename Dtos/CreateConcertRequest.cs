using System.ComponentModel.DataAnnotations;

namespace ConcertApi.Dtos;

public class CreateConcertRequest
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string Title { get; set; } = "";

    [Required, StringLength(200, MinimumLength = 2)]
    public string Artist { get; set; } = "";

    [Required, StringLength(200, MinimumLength = 2)]
    public string Venue { get; set; } = "";

    [Required, StringLength(120, MinimumLength = 2)]
    public string City { get; set; } = "";

    [Required]
    public DateTime ConcertDate { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }
}