using Domain.Models;

namespace BankAccountSimulatorMVC.ViewModels;

public class AccountsIndexViewModel
{
    public IReadOnlyList<BankAccount> Accounts { get; set; } = new List<BankAccount>();

    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }

    public int TotalPages => TotalItems <= 0
        ? 1
        : (int)Math.Ceiling(TotalItems / (double)PageSize);
}
