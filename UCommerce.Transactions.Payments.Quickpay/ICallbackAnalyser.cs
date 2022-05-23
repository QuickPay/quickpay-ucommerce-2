using System.Web;
using UCommerce.Transactions.Payments.Unzer.Models;

namespace UCommerce.Transactions.Payments.Unzer
{
    public interface ICallbackAnalyser
    {
        bool IsTestMode(ApiResponseDto apiResponseDto);
        ApiResponseDto ReadCallbackBody(HttpContext currentHttpContext);
    }
}