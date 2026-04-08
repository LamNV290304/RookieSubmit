using System;
using System.Collections.Generic;
using System.Text;

namespace BankAccountSimulator
{
    public class Transaction
    {
        public int TransactionId { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public TransactionType Type { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public string Description { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"Transaction ID: {TransactionId} | Account: {AccountNumber} | Date: {Date:yyyy-MM-dd HH:mm:ss} | Type: {Type} | Amount: {Amount:N2} | Description: {Description}";
        }
    }

    public enum TransactionType
    {
        Deposit,
        Withdrawal,
        TransferOut,
        TransferIn
    }
}
