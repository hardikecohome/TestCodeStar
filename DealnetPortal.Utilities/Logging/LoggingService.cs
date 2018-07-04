using System;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace DealnetPortal.Utilities.Logging
{
    /// <summary>
    /// Logging service implementation
    /// (log4net used)
    /// </summary>
    public class LoggingService : ILoggingService
    {
        private readonly log4net.ILog _logger;
     //   private readonly IEmailService _emailService;

        public LoggingService()
        {
            //_logger = log4net.LogManager.GetLogger(GetType());
            _logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
     //       _emailService = new EmailService();
        }

        public void LogInfo(string info)
        {
            _logger?.Info(info);
        }

        public void LogWarning(string warning)
        {
            _logger?.Warn(warning);
        }

        public void LogError(string error)
        {
            _logger?.Error(error);
            //_emailService.SendAsync(new List<string>() { ConfigurationManager.AppSettings["ErrorLogTo"] },
            //                            ConfigurationManager.AppSettings["ErrorLogFrom"],
            //                            ConfigurationManager.AppSettings["ErrorLogSubject"]
            //                            , error);
        }

        public void LogError(string error, Exception ex)
        {
            _logger?.Error(error, ex);
            //_emailService.SendAsync(new List<string>() { ConfigurationManager.AppSettings["ErrorLogTo"] },
            //                            ConfigurationManager.AppSettings["ErrorLogFrom"],
            //                            ConfigurationManager.AppSettings["ErrorLogSubject"]
            //                            , "Error Message : " +error+ " Exception Thrown : " + ex);
        }

    }
}
