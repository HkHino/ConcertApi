using System.IdentityModel.Tokens.Jwt;
using ConcertApi.Data;
using Microsoft.EntityFrameworkCore;

namespace ConcertApi.Auth;

public class RevocationMiddleware
{
    private readonly RequestDelegate _next;

    public RevocationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        var auth = context.Request.Headers.Authorization.ToString();
        if (auth.StartsWith("Bearer "))
        {
            var token = auth["Bearer ".Length..].Trim();
            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(token))
            {
                var jwt = handler.ReadJwtToken(token);
                var jti = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

                if (!string.IsNullOrWhiteSpace(jti))
                {
                    var revoked = await db.RevokedTokens.AsNoTracking()
                        .AnyAsync(x => x.Jti == jti);

                    if (revoked)
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsJsonAsync(new { message = "Token revoked." });
                        return;
                    }
                }
            }
        }

        await _next(context);
    }
}