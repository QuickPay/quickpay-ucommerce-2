using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using UCommerce.Api;
using UCommerce.EntitiesV2;
using UCommerce.Transactions.Payments.Unzer.Exceptions;
using UCommerce.Transactions.Payments.Unzer.Logger;
using UCommerce.Transactions.Payments.Unzer.Models;

namespace UCommerce.Transactions.Payments.Unzer
{
    public class UnzerClient : Quickpay.QuickPayRestClient, IDisposable
    {
        private readonly string apiKey;
        private HttpClient client;
        private readonly IMessageHelper messageHelper;
        private readonly IUnzerLogger logger;

        public UnzerClient(string apikey, string baseUrl, IMessageHelper messageHelper, IUnzerLogger logger) : base(apikey)
        {
            this.apiKey = apikey;
            this.messageHelper = messageHelper;
            this.logger = logger;
            client = new HttpClient();
            client.BaseAddress = this.Client.BaseUrl;
            client.DefaultRequestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(":" + apiKey)));
            client.DefaultRequestHeaders.Add("Accept-Version", "v10");
            client.DefaultRequestHeaders.Add("Accept", "application/json, application/xml, text/json, text/x-json, text/javascript, text/xml");
            client.DefaultRequestHeaders.Add("User-Agent", "Unzer .Net client");
        }

        public async Task<PaymentResponse> GetPaymentByOrderIdAsync(Order order)
        {
            var response = await client.GetAsync("/payments?order_id=" + order.Order_ID).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsAsync<List<PaymentResponse>>().ConfigureAwait(false);
                return result.FirstOrDefault();
            } else
            {
                return null;
            }
        }

        private async Task<ApiResponseDto> GetPaymentByTransactionIdAsync(Payment payment, ApiResponseDto oldResponseDto)
        {
            for (var attempts = 0; attempts < 3; attempts++)
            {
                var resource = string.Format("payments/{0}", payment.TransactionId);
                var responseDto = await GetResponseDtoAsync(resource, "GET").ConfigureAwait(false);
                if (responseDto.Operations != null)
                {
                    var operation = responseDto.Operations.LastOrDefault();
                    if (operation != null && !operation.pending)
                    {
                        oldResponseDto = responseDto;
                        break;
                    }
                }
                // Sleep for 1 second before trying again
                Thread.Sleep(1000);
            }

            return oldResponseDto;
        }

        public PaymentResponse Payment(decimal amount, string currency, Order order)
        {
            // ExecutePostProcessingPipeline

            var englishDecimal = (amount * new Decimal(100)).ToString("0.##", new System.Globalization.CultureInfo("en-US"));
            var values = new Dictionary<string, string>
            {
                { "amount", englishDecimal },
                { "currency", currency },
                { "order_id", order.Order_ID }
            };

            var content = new FormUrlEncodedContent(values);

            var response = client.PostAsync("/payments", content).ConfigureAwait(false).GetAwaiter().GetResult();
            var responseStr = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            var result = JsonConvert.DeserializeObject<PaymentResponse>(responseStr);
            return result;
        }

        public PaymentLinkUrl PaymentLink(GenerateLinkRequest model)
        {

            Action<RestRequest> prepareRequest = (RestRequest request) =>
            {
                var englishDecimal = (model.amount * new Decimal(100)).ToString("0.##", new System.Globalization.CultureInfo("en-US"));
                request.AddParameter("amount", englishDecimal)
                .AddParameter("continue_url", model.continue_url)
                .AddParameter("cancel_url", model.cancel_url)
                .AddParameter("callbackurl", model.callback_url)
                .AddParameter("customer_email", model.customer_email)
                .AddParameter("autocapture", model.autocapture)
                .AddParameter("language", model.language);

                if (!string.IsNullOrWhiteSpace(model.payment_methods))
                {
                    request.AddParameter("payment_methods", model.payment_methods);
                }

                request.Method = Method.PUT;
            };

            var result = this.CallEndpoint<PaymentLinkUrl>($"/payments/{model.id}/link", prepareRequest);

            return result;
        }

        public async Task<(bool valid, string status)> RefundPaymentAsync(Payment payment)
        {
            var resource = string.Format("payments/{0}/refund?amount={1}", payment.TransactionId, Convert.ToInt32(Decimal.Round(payment.Amount, 2) * new Decimal(100)));
            return await GetResponse(payment, resource, "POST").ConfigureAwait(false);
        }

        public async Task<(bool valid, string status)> CancelPaymentAsync(Payment payment)
        {
            var resource = string.Format("payments/{0}/cancel", payment.TransactionId);
            return await GetResponse(payment, resource, "POST").ConfigureAwait(false);
        }

        private async Task<ApiResponseDto> GetResponseDtoAsync(string resource, string method)
        {
            HttpResponseMessage request = null;

            if (method == "POST")
            {
                var values = new Dictionary<string, string>
                {
                };
                var content = new FormUrlEncodedContent(values);
                request = await client.PostAsync(resource, content).ConfigureAwait(false);
            } else
            {
                request = await client.GetAsync(resource).ConfigureAwait(false);
            }

            var dto = new ApiResponseDto();
            try
            {
                if (request.IsSuccessStatusCode)
                {
                    dto = await request.Content.ReadAsAsync<ApiResponseDto>().ConfigureAwait(false);
                    var operations = dto.Operations;
                    if (operations != null)
                    {
                        var operation = operations.LastOrDefault();
                        if (operation != null && !string.IsNullOrEmpty(operation.qp_status_code) &&
                            !string.IsNullOrEmpty(operation.type))
                        {
                            dto.StatusMessage = messageHelper.GetStatusMessage(operation.type, operation.qp_status_code, operation.qp_status_msg);

                            if (operation.qp_status_code.ToLower() != "20000")
                            {
                                dto.Accepted = false;
                            }
                        }
                    }
                } else
                {
                    if (request != null)
                    {
                        if (request.StatusCode == HttpStatusCode.Unauthorized ||
                            request.StatusCode == HttpStatusCode.BadRequest)
                        {
                            var response = await request.Content.ReadAsAsync<RequestFailed>().ConfigureAwait(false);
                            dto.StatusMessage = response.Message + $" - {request.StatusCode}";
                        }
                        else
                        {
                            dto.StatusMessage = $"Unknown error - {request.StatusCode}";
                        }
                    }
                    else
                    {
                        dto.StatusMessage = "No request found";
                    }

                    logger.LogException(new Exception(dto.StatusMessage));
                }
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw;
            }

            return dto;
        }

        private async Task<(bool accepted, string status)> GetResponse(Payment payment, string resoruceUrl, string method)
        {
            var response = await GetResponseDtoAsync(resoruceUrl, method).ConfigureAwait(false);
            var dto = await GetPaymentByTransactionIdAsync(payment, response).ConfigureAwait(false);
            return (dto.Accepted, dto.StatusMessage );
        }

        public async Task<(bool accepted, string status)> CapturePaymentAsync(Payment payment, bool autoCapture)
        {
            if (payment.Amount <= 0)
            {
                await CancelPaymentAsync(payment).ConfigureAwait(false);
                throw new Exception("Cannot capture when amount is zero or less");
            }

            if (payment.PaymentStatus == PaymentStatus.Get((int)PaymentStatusCode.Declined))
            {
                throw new Exception("Payment was declined.");
            };

            var resource = string.Format("payments/{0}/capture?amount={1}", payment.TransactionId, Convert.ToInt32(Decimal.Round(payment.Amount, 2) * new Decimal(100)));
            var method = "POST";

            // If payment has auto capture then check if payment has been captured
            if (autoCapture)
            {
                resource = string.Format("payments/{0}", payment.TransactionId);
                method = "GET";
            }

            var response = await GetResponse(payment, resource, method).ConfigureAwait(false);

            if (response.accepted == false && autoCapture)
            {
                throw new AutoCaptureException(payment, response.status);
            }

            if (response.accepted == false && !autoCapture)
            {
                throw new CaptureException(payment, response.status);
            }

            return (response.accepted, response.status);
        }

        public void Dispose()
        {
            client.Dispose();
        }
    }
}
