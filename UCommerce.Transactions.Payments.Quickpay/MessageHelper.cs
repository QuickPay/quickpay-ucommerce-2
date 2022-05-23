using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UCommerce.Transactions.Payments.Unzer
{
    public class MessageHelper : IMessageHelper
    {
        public string GetStatusMessage(string type, string statusCode, string statusMessage)
        {
            var message = "";
            type = type.ToLower();
            statusCode = statusCode.ToLower();

            if (statusCode == "20000")
            {
                switch (type)
                {
                    case "authorize":
                        message = "Payment authorized";
                        break;
                    case "capture":
                        message = "Payment captured";
                        break;
                    case "cancel":
                        message = "Payment cancelled";
                        break;
                    case "refund":
                        message = "Payment refunded";
                        break;
                }
            }
            else if (statusCode == "40000")
            {
                switch (type)
                {
                    case "refund":
                        message = $"Refund rejected by quickpay ({statusMessage})";
                        break;
                    case "cancel":
                        message = $"Cancel rejected by quickpay ({statusMessage})";
                        break;
                    case "capture":
                        message = $"Capture rejected by quickpay ({statusMessage})";
                        break;
                    default:
                        message = "Rejected By Acquirer. Try check your gateway, if user data is valid or if card has expired";
                        break;
                }
            }
            else if (statusCode == "40001")
            {
                message = "Request Data Error";
            }
            else if (statusCode == "50000")
            {
                message = "Gateway Error";
            }
            else if (statusCode == "50300")
            {
                message = "Communications Error (with Acquirer)";
            }
            else
            {
                switch (type)
                {
                    case "refund":
                        message = "Refund payment is pending. Try again later";
                        break;
                    case "cancel":
                        message = "Cancel payment is pending. Try again later";
                        break;
                    case "capture":
                        message = "Capture payment is pending. Try again later";
                        break;
                    default:
                        message = "Unknown Error";
                        break;
                }
            }
            return message;
        }
    }
}
