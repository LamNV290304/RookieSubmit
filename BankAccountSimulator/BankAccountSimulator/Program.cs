namespace BankAccountSimulator
{
    public class Program
    {
        static void Main(string[] args)
        {
            BankHelper.HandleMenu();
        }
    }

    public static class BankHelper
    {
        public static void ShowMenu()
        {
            Console.WriteLine("\n=== Bank Account System ===");
            Console.WriteLine("1. Create new account");
            Console.WriteLine("2. Deposit money");
            Console.WriteLine("3. Withdraw money");
            Console.WriteLine("4. Transfer money");
            Console.WriteLine("5. View account details");
            Console.WriteLine("6. View transaction history");
            Console.WriteLine("7. Freeze / Unfreeze account");
            Console.WriteLine("8. Calculate monthly interest");
            Console.WriteLine("9. Exit");
        }

        public static void HandleMenu()
        {
            BankAccountManage bankAccountManage = BankAccountManage.Instance;
            while (true)
            {
                ShowMenu();
                Console.Write("Select an option: ");
                var input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine("Input cannot be empty. Please try again.");
                    continue;
                }

                switch (input)
                {
                    case "1":
                        bankAccountManage.CreateAccount();
                        break;
                    case "2":
                        bankAccountManage.Deposit();
                        break;
                    case "3":
                        bankAccountManage.Withdraw();
                        break;
                    case "4":
                        bankAccountManage.Transfer();
                        break;
                    case "5":
                        bankAccountManage.ViewAccountDetails();
                        break;
                    case "6":
                        bankAccountManage.ViewTransactionHistory();
                        break;
                    case "7":
                        bankAccountManage.ChangeAccountStatus();
                        break;
                    case "8":
                        bankAccountManage.CalculateMonthlyInterest();
                        break;
                    case "9":
                        Console.WriteLine("Exit successful.");
                        return;
                    default:
                        Console.WriteLine("Invalid option. Please try again.");
                        break;
                }
            }
        }
    }
}
