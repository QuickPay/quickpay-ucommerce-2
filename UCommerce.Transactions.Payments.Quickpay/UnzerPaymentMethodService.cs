using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UCommerce.EntitiesV2;
using UCommerce.Extensions;
using UCommerce.Infrastructure.Globalization;
using Quickpay;
using System.Threading;
using System.Web;
using UCommerce.Transactions.Payments.Unzer.Models;
using UCommerce.Infrastructure;
using UCommerce.Transactions.Payments.Common;
using UCommerce.Transactions.Payments.Unzer.Logger;
using System.Security.Cryptography;
using UCommerce.Web;
using UCommerce.Transactions.Payments.Unzer.Exceptions;

namespace UCommerce.Transactions.Payments.Unzer
{
    /// <summary>
    /// https://docs.ucommerce.net/ucommerce/v8.1/payment-providers/integrating-a-payment-gateway.html
    /// </summary>
    public class UnzerPaymentMethodService : ExternalPaymentMethodService
    {
        public static string API_ENDPOINT_URL = "https://api.quickpay.net";
        private readonly IUnzerLogger logger;
        private readonly IOrderService orderService;
        private readonly IMessageHelper messageHelper;
        private readonly AbstractPageBuilder pageBuilder;

        public UnzerPaymentMethodService(Infrastructure.Logging.ILoggingService defaultLoggingService, 
            IOrderService orderService,
            UnzerPageBuilder pageBuilder)
        {
            this.logger = new UnzerLogger(defaultLoggingService);
            var baseUrl = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyBaseUrlAttribute>().BaseUrl;
            UnzerPaymentMethodService.API_ENDPOINT_URL = baseUrl;
            this.pageBuilder = pageBuilder;
            this.orderService = orderService;
            this.messageHelper = new MessageHelper();
        }

        protected Payment ChangeOrderStatus(Payment payment, string orderStatusName, PaymentStatus paymentStatus)
        {
            var newOrderStatus = OrderStatus.All().Single(x => x.Name == orderStatusName);
            this.orderService.ChangeOrderStatus(payment.PurchaseOrder, newOrderStatus);
            payment.PaymentStatus = paymentStatus;
            payment.Save();
            return payment;
        }

        public virtual Money CalculatePaymentFee(PaymentRequest request)
        {
            var order = request.PurchaseOrder;
            var paymentMethod = request.PaymentMethod;

            var fee = paymentMethod.GetFeeForCurrency(order.BillingCurrency);

            if (!order.SubTotal.HasValue)
                return new Money(0,
                    new CultureInfo(Globalization.CultureCode), order.BillingCurrency);

            return new Money(order.SubTotal.Value
                * (paymentMethod.FeePercent / 100)
                + fee.Fee,
                new CultureInfo(Globalization.CultureCode), order.BillingCurrency);
        }


        protected string Sign(string @base, string privateAccountKey)
        {
            var e = Encoding.UTF8;

            var hmac = new HMACSHA256(e.GetBytes(privateAccountKey));
            byte[] b = hmac.ComputeHash(e.GetBytes(@base));

            var s = new StringBuilder();
            for (int i = 0; i < b.Length; i++)
            {
                s.Append(b[i].ToString("x2"));
            }

            return s.ToString();
        }

        protected bool ValidatePayment(string privateAccountKey, ApiResponseDto callbackObject)
        {
            try
            {
                var currentHttpContext = HttpContext.Current;
                if (ValidateChecksum(currentHttpContext, privateAccountKey))
                {
                    // Get operations to check if payment has been approved
                    var operations = callbackObject.Operations.LastOrDefault();
                    // Check if payment has been approved
                    return operations != null && (operations.qp_status_code == "000" || operations.qp_status_code == "20000") && operations.qp_status_msg.ToLower() == "approved";
                }
            }
            catch (Exception ex)
            {
                logger.LogException(ex, "ValidatePayment Exception:  ");
            }
            return false;
        }

        protected bool ValidateChecksum(HttpContext context, string privateAccountKey)
        {
            string checkSum = context.Request.Headers["QuickPay-Checksum-Sha256"];

            var bytes = new byte[context.Request.InputStream.Length];
            context.Request.InputStream.Read(bytes, 0, bytes.Length);
            context.Request.InputStream.Position = 0;
            string content = Encoding.UTF8.GetString(bytes);

            string compute = Sign(content, privateAccountKey);

            var checksum = false;
            if (checkSum.Equals(compute))
            {
                checksum = true;
            }

            return checksum;
        }


        public override void ProcessCallback(Payment payment)
        {
            try
            {
                Guard.Against.Null(HttpContext.Current, "Missing httpcontext");

                var callbackObject = new CallbackAnalyser().ReadCallbackBody(HttpContext.Current);
                
                if (!(callbackObject.State == "processed" || callbackObject.State == "new"))
                {
                    return;
                }

                Guard.Against.PaymentNotPendingAuthorization(payment);

                var options = Options.Create(payment.PaymentMethod); // cb1a8e2308a1be1fbf22febd07fc8eb1a5d374a89b0d5c26e2bf2f1ca5669214
                if (ValidatePayment(options.PrivateAccountKey, callbackObject))
                {
                    if (callbackObject.test_mode && options.CancelTestCardOrders)
                    {

                        ChangeOrderStatus(payment, "Cancelled", PaymentStatus.Get((int)PaymentStatusCode.Cancelled));
                        logger.Log("Order (" + payment.PurchaseOrder.OrderNumber + ") was canceled because the payment was in testmode.");
                        return;
                    }

                    payment.PaymentStatus = PaymentStatus.Get((int)PaymentStatusCode.Authorized);
                    ProcessPaymentRequest(new PaymentRequest(payment.PurchaseOrder, payment));
                    UCommerce.Api.TransactionLibrary.Checkout();

                    if (options.AutoCapture)
                    {
                        ChangeOrderStatus(payment, "Completed order", PaymentStatus.Get((int)PaymentStatusCode.Acquired));
                    }
                }
                else
                {
                    logger.Log("Payment for order '" + payment.PurchaseOrder.OrderNumber + "' not validated");
                    payment.PaymentStatus = PaymentStatus.Get((int)PaymentStatusCode.Declined);
                    payment.Save(); //Save payment to ensure transactionId not lost.
                }
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
            }
        }

        protected override bool CancelPaymentInternal(Payment payment, out string status)
        {
            var options = Options.Create(payment.PaymentMethod);
            var client = new UnzerClient(options.ApiKey, API_ENDPOINT_URL, messageHelper, logger);

            var result = client.CancelPaymentAsync(payment).ConfigureAwait(false).GetAwaiter().GetResult();
            status = result.status;

            return result.valid;
        }


        protected override bool AcquirePaymentInternal(Payment payment, out string status)
        {
            var options = Options.Create(payment.PaymentMethod);
            var client = new UnzerClient(options.ApiKey, API_ENDPOINT_URL, messageHelper, logger);
            var valid = false;
            try
            {
                var result = client.CapturePaymentAsync(payment, options.AutoCapture).ConfigureAwait(false).GetAwaiter().GetResult();
                status = result.status;
                valid = result.accepted;
            } catch (AutoCaptureException ex)
            {
                status = ex.Message;
                valid = false;
                ChangeOrderStatus(ex.payment, "Requires attention", PaymentStatus.Get((int)PaymentStatusCode.AcquireFailed));
            }
            catch (CaptureException ex)
            {
                payment.PaymentStatus = PaymentStatus.Get((int)PaymentStatusCode.AcquireFailed);
                payment.Save();
                status = ex.Message;
                valid = false;
            }
            catch (Exception ex)
            {
                status = ex.Message;
                valid = false;
            }

            return valid;
        }

        protected override bool RefundPaymentInternal(Payment payment, out string status)
        {
            var options = Options.Create(payment.PaymentMethod);
            var client = new UnzerClient(options.ApiKey, API_ENDPOINT_URL, messageHelper, logger);

            var result = client.RefundPaymentAsync(payment).ConfigureAwait(false).GetAwaiter().GetResult();
            status = result.status;

            return result.valid;
        }

        public override string RenderPage(PaymentRequest paymentRequest)
        {
            return pageBuilder.Build(paymentRequest);
        }
    }
}
