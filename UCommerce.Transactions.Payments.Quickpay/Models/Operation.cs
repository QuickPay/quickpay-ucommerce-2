using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UCommerce.Transactions.Payments.Unzer.Models
{
    public class Operation
    {
        public string qp_status_msg { get; set; }
        public string qp_status_code { get; set; }
        public string aq_status_code { get; set; }

        public string aq_status_msg { get; set; }

        public string @type { get; set; }
        public decimal? amount { get; set; }

        public string callback_url { get; set; }

        public bool pending { get; set; }
    }
}
