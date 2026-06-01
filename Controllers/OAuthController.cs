using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ConcertApi.Auth;
using ConcertApi.Data;
using ConcertApi.Dtos.OAuth;
using ConcertApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConcertApi.Controllers;

[ApiController]
[Route("api/v1/oauth")]
public class OAuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtService _jwt;
    private readonly IConfiguration _config;

    public OAuthController(AppDbContext db, JwtService jwt, IConfiguration config)
    {
        _db = db;
        _jwt = jwt;
        _config = config;
    }

    // POST /api/v1/oauth/token
    [HttpPost("token")]
    public async Task<IActionResult> Token([FromBody] TokenRequest req)
    {
        var minutes = int.Parse(_config["Jwt:AccessTokenMinutes"]!);
        var expiresInSeconds = minutes * 60;

        if (req.GrantType == "password")
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { error = "invalid_request", error_description = "username/password required" });

            var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == req.Username);
            if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
                return Unauthorized(new { error = "invalid_grant", error_description = "invalid credentials" });

            var (accessToken, expiresAt, _) = _jwt.CreateToken(user);

            // Create refresh token (random) and store HASHED in DB
            var refreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) +
                               Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            var refreshHash = BCrypt.Net.BCrypt.HashPassword(refreshToken);

            _db.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = refreshHash,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            return Ok(new TokenResponse
            {
                access_token = accessToken,
                token_type = "Bearer",
                expires_in = expiresInSeconds,
                refresh_token = refreshToken
            });
        }

        if (req.GrantType == "refresh_token")
        {
            if (string.IsNullOrWhiteSpace(req.RefreshToken))
                return BadRequest(new { error = "invalid_request", error_description = "refresh_token required" });

            // Find a non-revoked refresh token that matches (verify hash)
            var candidates = await _db.RefreshTokens
                .Where(x => x.RevokedAt == null && x.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var match = candidates.FirstOrDefault(x => BCrypt.Net.BCrypt.Verify(req.RefreshToken, x.TokenHash));
            if (match is null)
                return Unauthorized(new { error = "invalid_grant", error_description = "invalid refresh token" });

            var user = await _db.Users.FirstAsync(u => u.Id == match.UserId);
            var (accessToken, _, _) = _jwt.CreateToken(user);

            return Ok(new TokenResponse
            {
                access_token = accessToken,
                token_type = "Bearer",
                expires_in = expiresInSeconds,
                refresh_token = null // optional: don’t rotate for simplicity
            });
        }

        return BadRequest(new { error = "unsupported_grant_type" });
    }

    // POST /api/v1/oauth/revoke
    // Revokes refresh token + blacklists current access token jti (if provided)
    [Authorize]
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RevokeRequest req)
    {
        // Revoke refresh token (matches by verifying hash)
        var candidates = await _db.RefreshTokens
            .Where(x => x.RevokedAt == null)
            .ToListAsync();

        var match = candidates.FirstOrDefault(x => BCrypt.Net.BCrypt.Verify(req.RefreshToken, x.TokenHash));
        if (match is not null)
            match.RevokedAt = DateTime.UtcNow;

        // Blacklist current access token (jti)
        var auth = Request.Headers.Authorization.ToString();
        if (auth.StartsWith("Bearer "))
        {
            var tokenString = auth["Bearer ".Length..].Trim();
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);

            var jti = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
            var exp = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp)?.Value;

            if (!string.IsNullOrWhiteSpace(jti) && !string.IsNullOrWhiteSpace(exp))
            {
                var expSeconds = long.Parse(exp);
                var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime;

                _db.RevokedTokens.Add(new RevokedToken
                {
                    Jti = jti,
                    ExpiresAt = expiresAt,
                    RevokedAt = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "revoked" });
    }
}