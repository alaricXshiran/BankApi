namespace BankApi.Dtos;

// What we send back after a successful login.
public class LoginResponseDto
{
    // The JWT the caller must send with every protected request.
    public string Token { get; set; } = string.Empty;

    // When the token stops working (in UTC), so the caller knows when to log in again.
    public DateTime ExpiresAt { get; set; }
}