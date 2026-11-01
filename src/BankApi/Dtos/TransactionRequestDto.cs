using System.ComponentModel.DataAnnotations;
namespace BankApi.Dtos;
public class TransactionRequestDto
{
    [Range(0.01,1_000_000,
        ErrorMessage ="Amount must be between LKR 0.01 and LKR 1,000,000")]
        public decimal Amount{get;set;}
}