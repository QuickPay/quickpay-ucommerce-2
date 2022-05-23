using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UCommerce.Transactions.Payments.Unzer.Models
{
    public class Order
    {
        [JsonProperty("invoice_address[name]")]
        public string Invoice_Name { get; set; }

        [JsonProperty("invoice_address[street]")]
        public string Invoice_Address { get; set; }

        [JsonProperty("invoice_address[house_number]")]
        public string Invoice_Address_House_Number { get; set; }

        [JsonProperty("invoice_address[house_extension]")]
        public string Invoice_Address_House_Extension { get; set; }

        [JsonProperty("invoice_address[city]")]
        public string Invoice_Address_City { get; set; }

        [JsonProperty("invoice_address[zip_code]")]
        public string Invoice_Address_Zip_Code { get; set; }

        [JsonProperty("invoice_address[country_code]")]
        public string Invoice_Address_Country { get; set; }

        [JsonProperty("invoice_address[phone_number]")]
        public string Invoice_Phone_Number { get; set; }

        [JsonProperty("invoice_address[email]")]
        public string Invoice_Email { get; set; }

        public string Order_ID { get; set; }
    }
}
