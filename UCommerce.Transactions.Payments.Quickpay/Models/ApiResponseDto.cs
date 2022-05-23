using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UCommerce.Transactions.Payments.Unzer.Models
{
    public class ApiResponseDto
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public List<Operation> Operations { get; set; }
        //public Metadata MetaData { get; set; }
        public string StatusMessage { get; set; }
        public string State { get; set; }

        public bool test_mode { get; set; }

        public bool Accepted { get; set; }
    }
}
