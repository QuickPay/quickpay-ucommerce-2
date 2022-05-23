using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UCommerce.EntitiesV2;
using UCommerce.Extensions;

namespace UCommerce.Transactions.Payments.Unzer.Models
{
    public class Options
    {
        public string ApiKey { get; set; }
        public string PrivateAccountKey { get; set; }
        public string Merchant { get; set; }
        public string Payment_Methods { get; set; }
        public string AgreementId { get; set; }
        public string ContinueUrl { get; set; }
        public string CancelUrl { get; set; }
        //public string CallbackUrl { get; set; }
        public bool AutoCapture { get; set; }
        public bool CancelTestCardOrders { get; set; }

        public Options()
        {

        }

        public static Options Create(PaymentMethod paymentMethod)
        {
            return new Options(paymentMethod);
        }

        public Options(PaymentMethod paymentMethod)
        {
            ApiKey = paymentMethod.DynamicProperty<string>().ApiKey;
            PrivateAccountKey = paymentMethod.DynamicProperty<string>().PrivateAccountKey;
            Merchant = paymentMethod.DynamicProperty<string>().Merchant;
            Payment_Methods = paymentMethod.DynamicProperty<string>().Payment_Methods;
            AgreementId = paymentMethod.DynamicProperty<string>().AgreementId;
            ContinueUrl = paymentMethod.DynamicProperty<string>().ContinueUrl;
            CancelUrl = paymentMethod.DynamicProperty<string>().CancelUrl;
            //CallbackUrl = paymentMethod.DynamicProperty<string>().CallbackUrl;
            AutoCapture = paymentMethod.DynamicProperty<bool>().AutoCapture;
            CancelTestCardOrders = paymentMethod.DynamicProperty<bool>().CancelTestCardOrders;
        }
    }
}
