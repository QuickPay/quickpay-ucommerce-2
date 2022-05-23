using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using UCommerce.Transactions.Payments.Unzer.Models;

namespace UCommerce.Transactions.Payments.Unzer
{
    public class CallbackAnalyser : ICallbackAnalyser
    {
        public CallbackAnalyser()
        {
        }

        public bool IsTestMode(ApiResponseDto apiResponseDto)
        {
            if (apiResponseDto == null) throw new ArgumentNullException("quickPayApiResponseDto");

            return apiResponseDto.test_mode;
        }

        public ApiResponseDto ReadCallbackBody(HttpContext currentHttpContext)
        {
            currentHttpContext.Request.InputStream.Position = 0;
            // Get quickpay callback body text - See parameters:http://tech.quickpay.net/api/callback/
            var bodyStream = new StreamReader(currentHttpContext.Request.InputStream);
            bodyStream.BaseStream.Seek(0, SeekOrigin.Begin);
            // Get body text
            var bodyText = bodyStream.ReadToEnd();
            currentHttpContext.Request.InputStream.Position = 0;
            // Deserialize json body text 
            return JsonConvert.DeserializeObject<ApiResponseDto>(bodyText);
        }
    }
}
