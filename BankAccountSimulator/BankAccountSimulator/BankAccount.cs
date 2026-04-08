using System;
using System.Collections.Generic;
using System.Text;

namespace BankAccountSimulator
{
    public class BankAccount
    {
        public string AccountNumber { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public Status Status { get; set; } = Status.Active;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public override string ToString()
        {
            return $"Account number: {AccountNumber} | Owner name: {OwnerName} | Balance: {Balance:N2} | Status: {Status} | Created date: {CreatedAt:yyyy-MM-dd HH:mm:ss}";
        }
    }

    public enum Status
    {
        Active,
        Frozen
    }
}
