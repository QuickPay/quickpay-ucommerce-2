using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UCommerce.Transactions.Payments.Unzer.Models
{
    public class GenerateLinkRequest
    {
        public string continue_url { get; set; }

        public decimal amount { get; set; }

        public string cancel_url { get; set; }

        public string payment_methods { get; set; }

        public string customer_email { get; set; }

        public string callback_url { get; set; }

        public string language { get; set; }

        public bool autocapture { get; set; }

        public string id { get; set; }
    }
}
