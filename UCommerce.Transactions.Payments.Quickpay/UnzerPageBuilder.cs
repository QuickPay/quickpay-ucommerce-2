using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using UCommerce.Content;
using UCommerce.EntitiesV2;
using UCommerce.Infrastructure;
using UCommerce.Transactions.Payments.Unzer.Logger;
using UCommerce.Transactions.Payments.Unzer.Models;
using UCommerce.Web;

namespace UCommerce.Transactions.Payments.Unzer
{
    public class UnzerPageBuilder : AbstractPageBuilder
    {
        private readonly IUnzerLogger logger;
        private readonly IMessageHelper messageHelper;
        private readonly ICallbackUrl callbackHelper;
        private readonly INumberSeriesService numberSeriesService;

        private IDomainService domainService { get; set; }

        public UnzerPageBuilder(IDomainService domainService,
            ICallbackUrl callbackHelper,
            Infrastructure.Logging.ILoggingService defaultLoggingService)
        {
            this.logger = new UnzerLogger(defaultLoggingService);
            this.callbackHelper = callbackHelper;
            this.domainService = domainService;
            this.messageHelper = new MessageHelper();
            this.numberSeriesService = ObjectFactory.Instance.Resolve<INumberSeriesService>();
        }

        protected override void BuildHead(StringBuilder page, PaymentRequest paymentRequest)
        {
            page.Append("<title>Unzer</title>");
        }

        protected virtual string GetTwoLetterLanguageName()
        {
            Domain domain = domainService.GetCurrentDomain();
            return domain != null
                ? domain.Culture.TwoLetterISOLanguageName.ToLower()
                : System.Threading.Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName.ToLower();
        }

        protected override void BuildBody(StringBuilder page, PaymentRequest paymentRequest)
        {
            if (paymentRequest == null) throw new ArgumentNullException("paymentRequest");

            var options = Options.Create(paymentRequest.PaymentMethod);

            if (string.IsNullOrWhiteSpace(options.Merchant))
            {
                throw new Exception("Missing merchant in configuraiton for uCommerce");
            }

            if (string.IsNullOrWhiteSpace(options.ApiKey))
            {
                throw new Exception("Missing apiKey in configuraiton for uCommerce");
            }

            var client = new UnzerClient(options.ApiKey, UnzerPaymentMethodService.API_ENDPOINT_URL, messageHelper, logger);

            if (string.IsNullOrEmpty(paymentRequest.PurchaseOrder.OrderNumber))
            {
                OrderNumberSerie orderNumberSerie = paymentRequest.PurchaseOrder.ProductCatalogGroup.OrderNumberSerie;
                if (orderNumberSerie == null)
                {
                    throw new InvalidOperationException($"Could not set order number for order as no OrderNumberSeries has been configured for store: {paymentRequest.PurchaseOrder.ProductCatalogGroup.Name}. Please select the store under the stores node in the backend and select the OrderNumber series.");
                }

                paymentRequest.PurchaseOrder.OrderNumber = numberSeriesService.GetNumber(orderNumberSerie.OrderNumberName);
                paymentRequest.PurchaseOrder.Save();
            }

            var order = new Models.Order()
            {
                Invoice_Address = paymentRequest.PurchaseOrder.BillingAddress.AddressName,
                Invoice_Address_Country = paymentRequest.PurchaseOrder.BillingAddress.Country.TwoLetterISORegionName,
                Invoice_Address_City = paymentRequest.PurchaseOrder.BillingAddress.City,
                Invoice_Address_Zip_Code = paymentRequest.PurchaseOrder.BillingAddress.PostalCode,
                Invoice_Email = paymentRequest.PurchaseOrder.BillingAddress.EmailAddress,
                Invoice_Name = paymentRequest.PurchaseOrder.BillingAddress.FirstName + " " + paymentRequest.PurchaseOrder.BillingAddress.LastName,
                Invoice_Phone_Number = paymentRequest.PurchaseOrder.BillingAddress.PhoneNumber,
                Order_ID = paymentRequest.PurchaseOrder.OrderNumber
            };

            PaymentResponse paymentResult = client.GetPaymentByOrderIdAsync(order).ConfigureAwait(false).GetAwaiter().GetResult();

            if (paymentResult == null)
            {
                paymentResult = client.Payment(paymentRequest.Amount.Value,
                    paymentRequest.Amount.Currency.ISOCode, order);
            }

            var operation = paymentResult.operations.LastOrDefault();
            if (operation != null && (operation.aq_status_msg == "Approved" || operation.qp_status_msg == "Approved"))
            {
                UCommerce.Runtime.SiteContext.Current.OrderContext.ClearBasketInformation();
                HttpContext.Current.Response.Redirect("/");
            }

            var callbackUrl = callbackHelper.GetCallbackUrl("(auto)", paymentRequest.Payment);
            var urlResult = client.PaymentLink(new Models.GenerateLinkRequest()
            {
                amount = paymentRequest.Amount.Value,
                continue_url = $"{options.ContinueUrl}?orderguid={paymentRequest.PurchaseOrder.OrderGuid}",
                cancel_url = $"{options.CancelUrl}?orderguid={paymentRequest.PurchaseOrder.OrderGuid}",
                callback_url = callbackUrl,
                customer_email = paymentRequest.PurchaseOrder.BillingAddress.EmailAddress,
                id = paymentResult.Id,
                payment_methods = $"{options.Payment_Methods}",
                autocapture = options.AutoCapture,
                language = paymentRequest.PurchaseOrder.CultureCode.Split('-')[0]
            });

            if (string.IsNullOrEmpty(paymentRequest.Payment.TransactionId))
            {
                if (!string.IsNullOrEmpty(paymentResult.Id)) paymentRequest.Payment.TransactionId = paymentResult.Id;
                // Cast exception if no access to quickpay api
                if (paymentRequest.Payment.TransactionId == null) throw new NotSupportedException("TransactionId is null");
            }
            paymentRequest.Payment.Save();

            if (String.IsNullOrWhiteSpace(urlResult.url))
            {
                var error = "The payment link url was null or empty.";
                logger.Log(error);
                throw new NotSupportedException(error);
            }

            page.Append(String.Format("<form id=\"Unzer\" name=\"Unzer\" action=\"{0}\">", urlResult.url));
            page.Append("</form>");
            page.Append(@"<script>document.getElementsByTagName('form')[0].submit();</script>");
        }
    }
}
