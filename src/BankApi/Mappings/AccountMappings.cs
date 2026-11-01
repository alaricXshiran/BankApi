using BankApi.Dtos;
using BankApi.Models;
namespace BankApi.Mappings;
public static class AccountMappings
{
    public static AccountDto ToDto(this Account account)
    {
        return new AccountDto
        {
            Id=account.Id,
            AccountNumber=account.AccountNumber,
            HolderName=account.HolderName,
            Type=account.Type,
            Balance=account.Balance,
            CreatedAt=account.CreatedAt
        };
    }
}