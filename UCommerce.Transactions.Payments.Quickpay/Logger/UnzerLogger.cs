using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UCommerce.Infrastructure.Logging;

namespace UCommerce.Transactions.Payments.Unzer.Logger
{
    public class UnzerLogger : IUnzerLogger
    {
        private readonly ILoggingService _defaultLoggingService;

        private readonly string _unzerLogDirectory;
        private readonly string _unzerLogFilename;

        public UnzerLogger(ILoggingService defaultLoggingService)
        {
            _defaultLoggingService = defaultLoggingService;
            _unzerLogDirectory = @"\App_Data\Logs\Unzer\";
            _unzerLogFilename = "Unzer.txt";

            Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + _unzerLogDirectory);
        }

        public void Log(string line)
        {
            using (var file = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + _unzerLogDirectory + _unzerLogFilename, true, Encoding.UTF8))
            {
                file.WriteLine(DateTime.Now.ToString(CultureInfo.InvariantCulture) + ": " + line);
            }
        }

        public void LogException(Exception exception, string message = null)
        {
            if (message != null) message = String.Empty;

            var logLine = "Exception: ";
            logLine += message;
            logLine += Environment.NewLine + exception.ToString();
            Log(logLine);
            _defaultLoggingService.Log<UnzerLogger>(exception, "Exception occurred in the Unzer UCommerce app: " + message);
        }
    }
}
