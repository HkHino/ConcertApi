namespace ConcertApi.Models;

public class RevokedToken
{
    public string Jti { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public DateTime RevokedAt { get; set; }
}