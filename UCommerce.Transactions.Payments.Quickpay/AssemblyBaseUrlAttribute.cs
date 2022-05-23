using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace UCommerce.Transactions.Payments.Unzer
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    [ComVisible(true)]
    public sealed class AssemblyBaseUrlAttribute : Attribute
    {
        public AssemblyBaseUrlAttribute(string baseUrl) : base()
        {
            this.BaseUrl = baseUrl;
        }

        public string BaseUrl { get; set; }
    }
}
