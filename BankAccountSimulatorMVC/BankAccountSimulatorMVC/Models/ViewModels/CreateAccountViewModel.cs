using System.ComponentModel.DataAnnotations;

namespace BankAccountSimulatorMVC.Models.ViewModels
{
    public class CreateAccountViewModel
    {
        [Required(ErrorMessage = "Account number is required.")]
        public string AccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Owner name is required.")]
        public string OwnerName { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Initial balance must be 0 or greater.")]
        public decimal InitialBalance { get; set; }
    }
}
