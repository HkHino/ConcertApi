using System.ComponentModel.DataAnnotations;

namespace ConcertApi.Dtos.OAuth;

public class RevokeRequest
{
    // Usually refresh token is revoked; access token revocation is handled via jti blacklist
    [Required]
    public string RefreshToken { get; set; } = "";
}