using System.ComponentModel.DataAnnotations;

namespace Domain.Models
{
    public class Transaction
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Account number is required.")]
        public string AccountNumber { get; set; } = string.Empty;

        public TransactionType Type { get; set; }

        [Range(1, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string Description { get; set; } = string.Empty;
    }

    public enum TransactionType
    {
        Deposit,
        Withdraw,
        Transfer
    }
}
