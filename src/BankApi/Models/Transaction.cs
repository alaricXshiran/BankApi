using System.Security.Cryptography.X509Certificates;

namespace BankApi.Models;

public class Transaction
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public DateTime CreatedAt { get; set; }

}