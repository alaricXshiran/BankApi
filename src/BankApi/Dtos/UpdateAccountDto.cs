using BankApi.Models;
namespace BankApi.Dto;

public class UpdateAccountDto
{
    public string holderName { get; set; } = string.Empty;
    public AccountType Type { get; set; }
}