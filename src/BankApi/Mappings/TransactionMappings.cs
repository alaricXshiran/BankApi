using System.Reflection.Metadata.Ecma335;
using BankApi.Dtos;
using BankApi.Models;
namespace BankApi.mappings;
public static class TransactionMappings
{
    public static TransactionDto ToDto(this Transaction transaction)
    {
        return new TransactionDto
        {
            Id=transaction.Id,
            Type=transaction.Type,
            Amount=transaction.Amount,
            BalanceAfter=transaction.BalanceAfter,
            CreatedAt=transaction.CreatedAt

        };
    }
}