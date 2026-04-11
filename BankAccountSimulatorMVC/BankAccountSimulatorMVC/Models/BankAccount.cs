using System.ComponentModel.DataAnnotations;

namespace BankAccountSimulatorMVC.Models
{
    public class BankAccount
    {
        [Required(ErrorMessage = "Account number is required.")]
        public string AccountNumber { get; set; } = string.Empty;

        [Required]
        public string OwnerName { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Balance must be 0 or positive")]
        public decimal Balance { get; set; }

        public AccountStatus Status { get; set; } = AccountStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public enum AccountStatus
    {
        Active,
        Frozen
    }
}
