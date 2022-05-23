using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using UCommerce.EntitiesV2;

namespace UCommerce.Transactions.Payments.Unzer.Exceptions
{
    public class CaptureException : Exception
    {
        public Payment payment { get; }
        public CaptureException(Payment payment)
        {
            this.payment = payment;
        }

        public CaptureException(Payment payment, string message) : base(message)
        {
            this.payment = payment;
        }

        public CaptureException(Payment payment, string message, Exception innerException) : base(message, innerException)
        {
            this.payment = payment;
        }

        protected CaptureException(Payment payment, SerializationInfo info, StreamingContext context) : base(info, context)
        {
            this.payment = payment;
        }
    }
}
