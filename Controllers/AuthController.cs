/*using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ConcertApi.Auth;
using ConcertApi.Data;
using ConcertApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConcertApi.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtService _jwt;

    public AuthController(AppDbContext db, JwtService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    public record LoginRequest(string Email, string Password);
    public record LoginResponse(string AccessToken, DateTime ExpiresAt, string RefreshToken);

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == req.Email);
        if (user is null) return Unauthorized(new { message = "Invalid credentials" });

        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid credentials" });

        var (accessToken, expiresAt, jti) = _jwt.CreateToken(user);

        // Create refresh token (random) and store HASHED in DB
        var refreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var refreshHash = BCrypt.Net.BCrypt.HashPassword(refreshToken);

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });

        await _db.SaveChangesAsync();

        return Ok(new LoginResponse(accessToken, expiresAt, refreshToken));
    }

    public record RefreshRequest(string RefreshToken);
    public record RefreshResponse(string AccessToken, DateTime ExpiresAt);

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
    {
        // find a non-revoked refresh token that matches (by verifying hash)
        var candidates = await _db.RefreshTokens
            .Where(x => x.RevokedAt == null && x.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var match = candidates.FirstOrDefault(x => BCrypt.Net.BCrypt.Verify(req.RefreshToken, x.TokenHash));
        if (match is null) return Unauthorized(new { message = "Invalid refresh token" });

        var user = await _db.Users.FirstAsync(u => u.Id == match.UserId);
        var (accessToken, expiresAt, _) = _jwt.CreateToken(user);

        return Ok(new RefreshResponse(accessToken, expiresAt));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        // Extract jti + exp from the current access token
        var auth = Request.Headers.Authorization.ToString();
        if (!auth.StartsWith("Bearer ")) return Unauthorized();

        var tokenString = auth["Bearer ".Length..].Trim();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);

        var jti = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
        var exp = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp)?.Value;

        if (string.IsNullOrWhiteSpace(jti) || string.IsNullOrWhiteSpace(exp))
            return BadRequest(new { message = "Token missing jti/exp" });

        var expSeconds = long.Parse(exp);
        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime;

        // Revoke access token
        _db.RevokedTokens.Add(new RevokedToken
        {
            Jti = jti,
            ExpiresAt = expiresAt,
            RevokedAt = DateTime.UtcNow
        });

        // Revoke ALL refresh tokens for the current user (clean logout)
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var refreshTokens = await _db.RefreshTokens.Where(x => x.UserId == userId && x.RevokedAt == null).ToListAsync();
        foreach (var rt in refreshTokens)
            rt.RevokedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Logged out (access + refresh revoked)" });
    }
}*/