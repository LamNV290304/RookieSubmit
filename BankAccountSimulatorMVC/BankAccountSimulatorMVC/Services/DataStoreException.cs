namespace BankAccountSimulatorMVC.Services
{
    public class DataStoreException : Exception
    {
        public DataStoreException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
