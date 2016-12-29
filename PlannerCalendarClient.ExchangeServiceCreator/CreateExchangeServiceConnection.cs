using System;
using System.Configuration;
using System.Net;
using Microsoft.Exchange.WebServices.Data;
using PlannerCalendarClient.Logging;

namespace PlannerCalendarClient.ExchangeServiceCreator
{
    // This sample is for demonstration purposes only. Before you run this sample, make sure 
    // that the code meets the coding requirements of your organization.
    public static class CreateExchangeServiceConnection
    {
        private static readonly ILogger Logger = Logging.Logger.GetLogger();

        static CreateExchangeServiceConnection()
        {
            // Setup the ServicePointManager.ServerCertificateValidationCallback
            CertificateCallback.Initialize();

            // Setup properties on the ServicePointManager to handle multiple long standing http calls in EWS.
            // The default value of 2 might cause problem with multiple long standing EWS.
            ServicePointManager.DefaultConnectionLimit = Config.ServicePointManagerDefaultConnectionLimit;
            Logger.LogDebug(LoggingEvents.DebugEvent.ServicePointManagerDefaultConnectionLimit(ServicePointManager.DefaultConnectionLimit));
        }

        // The following is a basic redirection validation callback method. It 
        // inspects the redirection URL and only allows the Service object to 
        // follow the redirection link if the URL is using HTTPS. 
        //
        // This redirection URL validation callback provides sufficient security
        // for development and testing of your application. However, it may not
        // provide sufficient security for your deployed application. You should
        // always make sure that the URL validation callback method that you use
        // meets the security requirements of your organization.
        internal static bool RedirectionUrlValidationCallback(string redirectionUrl)
        {
            // The default for the validation callback is to reject the URL.
            bool result = false;

            var redirectionUri = new Uri(redirectionUrl);

            // Validate the contents of the redirection URL. In this simple validation
            // callback, the redirection URL is considered valid if it is using HTTPS
            // to encrypt the authentication credentials. 
            if (redirectionUri.Scheme == "https")
            {
                Logger.LogDebug(LoggingEvents.DebugEvent.RedirectionAllow(redirectionUrl));
                result = true;
            }

            Logger.LogDebug(LoggingEvents.DebugEvent.RedirectionDisallow(redirectionUrl));
            return result;
        }

        /// <summary>
        /// Return an exchange service object for EWS
        /// </summary>
        /// <returns>The exchange service object for EWS</returns>
        public static ExchangeService ConnectToService(string[] args)
        {
            var exchangeConfigData = Config.GetConnectionConfig(args);

            return ConnectToService(exchangeConfigData, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="exchangeConnectionConfig"></param>
        /// <param name="mailAffinity"></param>
        /// <param name="listener"></param>
        /// <returns></returns>
        public static ExchangeService ConnectToService(IExchangeConnectionConfig exchangeConnectionConfig, string mailAffinity = null, ITraceListener listener = null)
        {
            if (exchangeConnectionConfig == null)
            {
                throw new ConfigurationErrorsException("Missing Exchange configuration data!");
            }

            LogExchangeConnectionConfiguration(exchangeConnectionConfig);

            var exchangeService = new ExchangeService(exchangeConnectionConfig.Version);

            Logger.LogDebug(LoggingEvents.DebugEvent.ConnectedToExchange());

            SetupLogging(exchangeService, exchangeConnectionConfig.EwsApiTraceFlags, listener);

            // DO this ??
            // exchangeService.PreAuthenticate = true;

            if (exchangeConnectionConfig.UseDefaultCredentials)
            {
                Logger.LogDebug(LoggingEvents.DebugEvent.UseDefaultCredentials(Environment.UserDomainName, Environment.UserName));
                exchangeService.UseDefaultCredentials = true;
            }
            else
            {
                var cred = new NetworkCredential(exchangeConnectionConfig.ServerUserEmailAccount, exchangeConnectionConfig.Password);
                exchangeService.Credentials = new WebCredentials(exchangeConnectionConfig.ServerUserEmailAccount, cred.Password);
                Logger.LogDebug(LoggingEvents.DebugEvent.UseWebCredentials(exchangeConnectionConfig.ServerUserEmailAccount)); 
            }

            if (exchangeConnectionConfig.AutodiscoverUrl == null)
            {
                if (mailAffinity == null)
                {
                    mailAffinity = exchangeConnectionConfig.ServerUserEmailAccount;
                    if (mailAffinity == null)
                    {
                        throw new ExchangeCreateServiceException(LoggingEvents.ErrorEvent.ExchangeAffinityMailAccountIsMissing());
                    }
                }

                Logger.LogDebug(LoggingEvents.DebugEvent.AutoDiscoverStart(mailAffinity));
                
                exchangeService.AutodiscoverUrl(mailAffinity, RedirectionUrlValidationCallback);
                exchangeConnectionConfig.AutodiscoverUrl = exchangeService.Url;
                Logger.LogDebug(LoggingEvents.DebugEvent.AutoDiscoverSetEwsUrl(exchangeService.Url.AbsoluteUri));
            }
            else
            {
                exchangeService.Url = exchangeConnectionConfig.AutodiscoverUrl;
                Logger.LogDebug(LoggingEvents.DebugEvent.ConfigurationSetEwsUrl(exchangeService.Url.AbsoluteUri)); 
            }

            exchangeService.EnableScpLookup = exchangeConnectionConfig.EnableScpLookup;

            return exchangeService;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="exchangeConnectionConfig"></param>
        /// <param name="impersonatedUserSMTPAddress"></param>
        /// <param name="listener"></param>
        /// <returns></returns>
        public static ExchangeService ConnectToServiceWithImpersonation(
          IExchangeConnectionConfig exchangeConnectionConfig,
          string impersonatedUserSMTPAddress,
          ITraceListener listener = null)
        {
            if (exchangeConnectionConfig == null)
            {
                throw new ConfigurationErrorsException("Missing Exchange configuration data!");
            }

            LogExchangeConnectionConfiguration(exchangeConnectionConfig);

            ExchangeService exchangeService = new ExchangeService(exchangeConnectionConfig.Version);

            SetupLogging(exchangeService, exchangeConnectionConfig.EwsApiTraceFlags, listener);

            exchangeService.Credentials = new NetworkCredential(exchangeConnectionConfig.ServerUserEmailAccount, exchangeConnectionConfig.Password);

            ImpersonatedUserId impersonatedUserId = new ImpersonatedUserId(ConnectingIdType.SmtpAddress, impersonatedUserSMTPAddress);

            exchangeService.ImpersonatedUserId = impersonatedUserId;

            if (exchangeConnectionConfig.AutodiscoverUrl == null)
            {
                exchangeService.AutodiscoverUrl(exchangeConnectionConfig.ServerUserEmailAccount, RedirectionUrlValidationCallback);
                exchangeConnectionConfig.AutodiscoverUrl = exchangeService.Url;
            }
            else
            {
                exchangeService.Url = exchangeConnectionConfig.AutodiscoverUrl;
            }

            exchangeService.EnableScpLookup = exchangeConnectionConfig.EnableScpLookup;

            return exchangeService;
        }

        #region dublicated code from GroupAffinitySolver

        private static void LogExchangeConnectionConfiguration(IExchangeConnectionConfig exchangeConnectionConfig)
        {
            Logger.LogDebug(LoggingEvents.DebugEvent.ExchangeConnectionConfigSetting(
                exchangeConnectionConfig.Version,
                exchangeConnectionConfig.UseDefaultCredentials,
                exchangeConnectionConfig.ServerUserEmailAccount,
                GetPasswordPrintout(exchangeConnectionConfig.Password),
                exchangeConnectionConfig.AutodiscoverUrl,
                exchangeConnectionConfig.EnableScpLookup,
                exchangeConnectionConfig.EwsApiTraceFlags,
                exchangeConnectionConfig.UseImpersonation));
        }

        private static void SetupLogging(ExchangeService exchangeService, TraceFlags traceFlag, ITraceListener listener)
        {
            Logger.LogDebug(LoggingEvents.DebugEvent.EwsTraceFlagSetting(traceFlag));

            if (traceFlag != TraceFlags.None)
            {
                exchangeService.TraceListener = listener ?? new LogTraceListener();
                exchangeService.TraceFlags = traceFlag;
                exchangeService.TraceEnabled = true;
            }
        }

        internal static string GetPasswordPrintout(System.Security.SecureString secureString)
        {
            if (secureString != null && secureString.Length != 0)
            {
                return "******";
            }
            else
            {
                return "(none)";
            }
        }

        #endregion dublicated code from GroupAffinitySolver
    
    }
}
