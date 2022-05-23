namespace UCommerce.Transactions.Payments.Unzer
{
    public interface IMessageHelper
    {
        string GetStatusMessage(string type, string statusCode, string statusMessage);
    }
}