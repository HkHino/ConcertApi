namespace ConcertApi.Dtos.OAuth;

public class TokenResponse
{
    public string access_token { get; set; } = "";
    public string token_type { get; set; } = "Bearer";
    public int expires_in { get; set; }         // seconds
    public string? refresh_token { get; set; }  // returned on password grant
}