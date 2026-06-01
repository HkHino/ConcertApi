using System.ComponentModel.DataAnnotations;

namespace ConcertApi.Dtos.OAuth;

public class TokenRequest
{
    [Required]
    public string GrantType { get; set; } = ""; // "password" | "refresh_token"

    // password grant
    public string? Username { get; set; }       // email
    public string? Password { get; set; }

    // refresh_token grant
    public string? RefreshToken { get; set; }
}