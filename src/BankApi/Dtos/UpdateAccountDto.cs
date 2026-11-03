using System.ComponentModel.DataAnnotations;
using BankApi.Models;
namespace BankApi.Dto;

public class UpdateAccountDto
{
    [Required(ErrorMessage = "Holder name is required.")]
    [StringLength(100, MinimumLength = 2,
        ErrorMessage = "Holder name must be between 2 and 100 characters.")]
    public string holderName { get; set; } = string.Empty;
    
    [EmailAddress(ErrorMessage = "Email address is not valid.")]
    [StringLength(254,
         ErrorMessage = "Email must be at most 254 characters.")]
    public string? Email { get; set; }

    [EnumDataType(typeof(AccountType),
        ErrorMessage = "Account type must be Savings or Current.")]
    public AccountType Type { get; set; }
}