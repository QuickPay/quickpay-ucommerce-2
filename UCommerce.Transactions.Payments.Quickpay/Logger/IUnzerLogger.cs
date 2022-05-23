using System;

namespace UCommerce.Transactions.Payments.Unzer.Logger
{
    public interface IUnzerLogger
    {
        void Log(string line);
        void LogException(Exception exception, string message = null);
    }
}