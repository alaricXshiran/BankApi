using System.ComponentModel.DataAnnotations;
using BankApi.Models;

namespace BankApi.Dtos;

public class AccountQueryParameters
{
    [StringLength(100, ErrorMessage = "Search must be at most 100 characters.")]
    public string? Search { get; set; }

    [EnumDataType(typeof(AccountType),
        ErrorMessage = "Account type must be Savings or Current.")]
    public AccountType? Type { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Page must be 1 or greater.")]
    public int Page { get; set; } = 1;

    [Range(1, 50, ErrorMessage = "Page size must be between 1 and 50.")]
    public int PageSize { get; set; } = 10;
}