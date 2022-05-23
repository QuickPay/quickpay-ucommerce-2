using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UCommerce.Transactions.Payments.Unzer.Models
{
    public class PaymentResponse
    {
        public string Id { get; set; }

        public string accepted { get; set; }

        public Operation[] operations { get; set; }
    }
}
