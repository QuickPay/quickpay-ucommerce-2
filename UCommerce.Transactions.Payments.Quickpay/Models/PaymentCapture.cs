using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UCommerce.Transactions.Payments.Unzer.Models
{
    public class PaymentCapture
    {
        public bool accepted { get; set; }

        public string order_id { get; set; }

        public decimal amount { get; set; }

        public string state { get; set; }

        public Operation[] operations { get; set; }
    }
}
