using System.ComponentModel.DataAnnotations;

namespace BankAccountSimulatorMVC.ViewModels
{
    public class DepositWithdrawViewModel
    {
        [Required(ErrorMessage = "Account number is required.")]
        public string AccountNumber { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }
    }
}
