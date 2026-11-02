using BankApi.Models;
namespace BankApi.Dtos;
public class TransactionDto
{
    public int Id{get;set;}
    public TransactionType Type{get;set;}
    public decimal Amount{get;set;}
    public decimal BalanceAfter{get;set;}
    public DateTime CreatedAt{get;set;}
}