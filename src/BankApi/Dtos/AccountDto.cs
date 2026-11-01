using System.Data;
using BankApi.Models;
namespace BankApi.Dtos;
//data transfer object DTO
public class AccountDto
{
    public int Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string HolderName { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }

}

