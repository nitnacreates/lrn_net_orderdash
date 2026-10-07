using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Central.Api.Endpoints;

/// <summary>
/// Single seeded admin user (§12) -> JWT. Kept lean (config-backed, no Identity tables) for the MVP.
/// </summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/login", (LoginRequest request, IConfiguration config) =>
        {
            var username = config["Dashboard:Username"] ?? "admin";
            var password = config["Dashboard:Password"] ?? "admin";
            if (request.Username != username || request.Password != password)
            {
                return Results.Unauthorized();
            }

            var key = config["Jwt:Key"] ?? "dev-jwt-signing-key-change-me-please-0123456789";
            var token = new JwtSecurityToken(
                issuer: config["Jwt:Issuer"] ?? "orderdash",
                audience: config["Jwt:Audience"] ?? "orderdash-dashboard",
                claims: [new Claim(ClaimTypes.Name, username), new Claim(ClaimTypes.Role, "admin")],
                expires: DateTime.UtcNow.AddHours(12),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));

            return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
        });
    }
}

public record LoginRequest(string Username, string Password);
