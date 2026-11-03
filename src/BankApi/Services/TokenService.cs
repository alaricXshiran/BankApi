using System.Security.Claims;
using System.Text;
using BankApi.Dtos;
using BankApi.Models;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BankApi.Services;

// Creates signed JWTs for users who have logged in successfully.
public class TokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public LoginResponseDto CreateToken(User user)
    {
        // Read the JWT settings (the key comes from User Secrets, the rest from appsettings.json).
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT key is not configured.");
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];
        var expiryMinutes = _configuration.GetValue<int>("Jwt:ExpiryMinutes");

        // Claims: harmless facts about the user stored inside the token.
        // Never put secrets here, because anyone holding the token can read them.
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // The "wax seal": sign the token with our secret key using HMAC-SHA256.
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        // Describe the token: who it's for, what it says, when it expires, and how it's signed.
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = issuer,
            Audience = audience,
            Expires = expiresAt,
            SigningCredentials = credentials
        };

        // Build the final token string: header.payload.signature
        var token = new JsonWebTokenHandler().CreateToken(tokenDescriptor);

        return new LoginResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt
        };
    }
}