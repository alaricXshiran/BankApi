using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;
using BankApi.Models;
namespace BankApi.Dtos;

public class CreateAccountDto
{
    [Required(ErrorMessage = "holder name is Required.")]
    [StringLength(100, MinimumLength = 2,
        ErrorMessage = "holder name must be between 2 and 100 characters.")]
    public string HolderName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email address is not valid.")]
    [StringLength(254,
        ErrorMessage = "Email must be at most 254 characters.")]
    public string? Email { get; set; }

    [EnumDataType(typeof(AccountType),
        ErrorMessage = "Holder name must be between 2 and 100 characters.")]
    public AccountType Type { get; set; }

    [Range(1000, 10_000_000,
        ErrorMessage = "Initial Deposit must be between LKR 1000 and LKR 10,000,000")]
    public decimal InitialDeposit { get; set; }
}