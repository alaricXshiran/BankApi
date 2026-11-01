using System.Security.Cryptography.X509Certificates;
using BankApi.Models;
namespace BankApi.Dtos;
public class CreateAccountDto
{
    public string HolderName{get; set;}=string.Empty;
    public AccountType Type {get; set;}
    public decimal InitialDeposit{get; set;}
}