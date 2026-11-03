using System.ComponentModel.DataAnnotations;

namespace BankApi.Dtos;

// What a staff member sends to log in.
public class LoginRequestDto
{
    // Max lengths stop someone from sending huge values to slow the server down.
    [Required(ErrorMessage = "Username is required.")]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}