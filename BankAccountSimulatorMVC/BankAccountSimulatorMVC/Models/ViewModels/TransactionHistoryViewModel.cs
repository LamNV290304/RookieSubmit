namespace BankAccountSimulatorMVC.Models.ViewModels
{
    public class TransactionHistoryViewModel
    {
        public string? AccountNumber { get; set; }
        public TransactionFilter Filter { get; set; } = TransactionFilter.All;
        public IReadOnlyList<Transaction> Transactions { get; set; } = new List<Transaction>();
    }

    public enum TransactionFilter
    {
        All,
        Deposits,
        Withdrawals,
        Transfers
    }
}
