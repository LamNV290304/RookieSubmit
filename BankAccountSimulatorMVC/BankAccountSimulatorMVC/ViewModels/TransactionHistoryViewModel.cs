using Domain.Models;

namespace BankAccountSimulatorMVC.ViewModels
{
    public class TransactionHistoryViewModel
    {
        public string? AccountNumber { get; set; }
        public TransactionFilter Filter { get; set; } = TransactionFilter.All;
        public IReadOnlyList<Transaction> Transactions { get; set; } = new List<Transaction>();

        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }

        public int TotalPages => TotalItems <= 0
            ? 1
            : (int)Math.Ceiling(TotalItems / (double)PageSize);
    }

    public enum TransactionFilter
    {
        All,
        Deposits,
        Withdrawals,
        Transfers
    }
}
