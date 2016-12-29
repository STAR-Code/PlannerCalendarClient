using System;
using System.Linq;
using System.Net;
using PlannerCalendarClient.ExchangeServiceCreator;
using PlannerCalendarClient.Logging;
using AEWS = Microsoft.Exchange.WebServices.Autodiscover;
// Only for the ITraceListener, ServiceResponseException and TraceFlags type 
using EWS = Microsoft.Exchange.WebServices.Data;

namespace PlannerCalendarClient.ExchangeStreamingService.Affinity
{
    /// <summary>
    /// This class analyse the the subscriber's exchange affinity
    /// </summary>
    class GroupAffinitySolver
    {
        private static readonly ILogger Logger = Logging.Logger.GetLogger();

        #region MailGroupAffinity class

        /// <summary>
        /// The group affinity information for a mail address
        /// </summary>
        public class MailGroupAffinity
        {
            private MailGroupAffinity(string mailAddress)
            {
                MailAddress = mailAddress;
                UpdateDate = DateTime.Now;
            }

            internal MailGroupAffinity(string mailAddress, string grpInfo, string extOutlookUrl)
                : this(mailAddress)
            {
                GroupingInformation = grpInfo;
                ExternalEwsUrl = extOutlookUrl;
            }

            internal MailGroupAffinity(string mailAddress, EventIdBase errorEvent, string errorMsg)
                : this(mailAddress)
            {
                ErrorEvent = errorEvent;
                ErrorMessage = errorMsg;
                GroupingInformation = "";
                ExternalEwsUrl = "";
            }

            internal MailGroupAffinity(string mailAddress, EventIdBase errorEvent, AEWS.AutodiscoverErrorCode exchangeErrorCode, string errorMsg)
                : this(mailAddress)
            {
                ErrorEvent = errorEvent;
                ErrorCode = exchangeErrorCode;
                ErrorMessage = errorMsg;
                GroupingInformation = "";
                ExternalEwsUrl = "";
            }

            public string MailAddress { get; private set; }
            public string GroupingInformation { get; private set; }
            public string ExternalEwsUrl { get; private set; }

            public string GroupingKey { get { return GroupingInformation + " " + ExternalEwsUrl; } }

            /// <summary>
            /// If the error event value is not set then this property is null.
            /// </summary>
            public EventIdBase ErrorEvent { get; private set; }
            /// <summary>
            /// If there is no exchange/ews error this property has the value of AutodiscoverErrorCode.NoError
            /// </summary>
            public AEWS.AutodiscoverErrorCode ErrorCode { get; private set; }
            public string ErrorMessage { get; private set; }
            public DateTime? UpdateDate { get; private set; }
        }

        #endregion MailGroupAffinity class

        private const int _DefaultMaxHops = 10;
        private readonly AEWS.AutodiscoverService _autodiscoverService;

        //public GroupAffinitySolver(ExchangeVersion exchangeVersion, TraceFlags ewsApiTraceFlags)
        //{
        //    Logger.LogDebug(LoggingEvents.DebugEvent.CreateGroupAffinitySolver, exchangeVersion.ToString());

        //    _autodiscoverService = new AutodiscoverService(exchangeVersion);
        //    SetupLogging(_autodiscoverService, ewsApiTraceFlags, null);
        //}

        //public GroupAffinitySolver(Uri uri, string domain, ExchangeVersion exchangeVersion, TraceFlags ewsApiTraceFlags)
        //{
        //    if (uri != null)
        //    {
        //        Logger.LogDebug(LoggingEvents.DebugEvent.CreateGroupAffinitySolver, SafeStringFormat.SafeFormat("Uri: \"{0}\", Version: \"{1}\"", uri, exchangeVersion.ToString()));
        //        _autodiscoverService = new AutodiscoverService(uri, exchangeVersion);
        //    }
        //    else if(domain!= null)
        //    {
        //        Logger.LogDebug(LoggingEvents.DebugEvent.CreateGroupAffinitySolver, SafeStringFormat.SafeFormat("domain: \"{0}\", Version: \"{1}\"", domain, exchangeVersion.ToString()));
        //        _autodiscoverService = new AutodiscoverService(domain, exchangeVersion);
        //    }
        //    else
        //    {
        //        Logger.LogDebug(LoggingEvents.DebugEvent.CreateGroupAffinitySolver, SafeStringFormat.SafeFormat("Version: \"{0}\"", exchangeVersion.ToString()));
        //        _autodiscoverService = new AutodiscoverService(exchangeVersion);
        //    }

        //    SetupLogging(_autodiscoverService, ewsApiTraceFlags, null);
        //}

        public GroupAffinitySolver(IExchangeConnectionConfig exchangeConnectionConfig)
        {
            if (exchangeConnectionConfig == null) throw new ArgumentNullException("exchangeConnectionConfig");

            if (exchangeConnectionConfig.AutodiscoverUrl != null)
            {
                Logger.LogDebug(LoggingEvents.DebugEvent.CreateGroupAffinitySolver(SafeStringFormat.SafeFormat("Uri: \"{0}\", Version: \"{1}\"", exchangeConnectionConfig.AutodiscoverUrl, exchangeConnectionConfig.Version)));
                _autodiscoverService = new AEWS.AutodiscoverService(exchangeConnectionConfig.AutodiscoverUrl, exchangeConnectionConfig.Version);
            }
            else if(!string.IsNullOrWhiteSpace(exchangeConnectionConfig.ServerUserEmailAccount))
            {
                 var v = exchangeConnectionConfig.ServerUserEmailAccount.Split('@');
                 var domain = (v.Count() == 2) ? v[1] : v[0];

                 Logger.LogDebug(LoggingEvents.DebugEvent.CreateGroupAffinitySolver(SafeStringFormat.SafeFormat("domain: \"{0}\", Version: \"{1}\"", domain, exchangeConnectionConfig.Version)));
                 _autodiscoverService = new AEWS.AutodiscoverService(domain, exchangeConnectionConfig.Version);
            }
            else
            {
                Logger.LogDebug(LoggingEvents.DebugEvent.CreateGroupAffinitySolver(SafeStringFormat.SafeFormat("Version: \"{0}\"", exchangeConnectionConfig.Version)));
                _autodiscoverService = new AEWS.AutodiscoverService(exchangeConnectionConfig.Version);
            }

            if (!exchangeConnectionConfig.UseDefaultCredentials)
            {
                //var cred = new NetworkCredential(exchangeConnectionConfig.ServerUserEmailAccount, exchangeConnectionConfig.Password);
                //exchangeService.Credentials = new NetworkCredential(ExchangeConnectionConfig.ServerUserEmailAccount, ExchangeConnectionConfig.Password);
                //exchangeService.Credentials = new WebCredentials(exchangeConnectionConfig.ServerUserEmailAccount, cred.Password);
                //_autodiscoverService.Credentials = cred;// new WebCredentials(exchangeConnectionConfig.ServerUserEmailAccount, cred.Password);
                Logger.LogDebug(LoggingEvents.DebugEvent.CreateGroupAffinitySolverLogin(exchangeConnectionConfig.ServerUserEmailAccount, GetPasswordPrintout(exchangeConnectionConfig.Password)));
                _autodiscoverService.Credentials = new NetworkCredential(exchangeConnectionConfig.ServerUserEmailAccount, exchangeConnectionConfig.Password);
            }
            else
            {
                Logger.LogDebug(LoggingEvents.DebugEvent.CreateGroupAffinitySolverUserDefaultCredential(Environment.UserDomainName, Environment.UserName));
            }

            _autodiscoverService.EnableScpLookup = exchangeConnectionConfig.EnableScpLookup;

            SetupLogging(_autodiscoverService, exchangeConnectionConfig.EwsApiTraceFlags, null);
        }

        public GroupAffinitySolver(AEWS.AutodiscoverService autodiscoverService)
        {
            Logger.LogDebug(LoggingEvents.DebugEvent.CreateGroupAffinitySolver("Full AutodiscoverService"));
            _autodiscoverService = autodiscoverService;
        }

        /// <summary>
        /// Solve the group affinity for the mail.
        /// 
        /// Note: All types of exceptions that can occured when trying to resolve the group affinity for the mail address
        ///       are returned in the MailGroupAffinity object. 
        /// </summary>
        /// <param name="emailAddress">The ail account to get the affinity for</param>
        /// <returns></returns>
        public MailGroupAffinity GetMailAccountGroupingInformationSettings(string emailAddress)
        {
            try
            {
                var response = GetUserSettings(
                    emailAddress,
                    _DefaultMaxHops,
                    AEWS.UserSettingName.GroupingInformation,
                    AEWS.UserSettingName.ExternalEwsUrl);

                if (response.ErrorCode == 0)
                {
                    var grpInfo =
                        response.Settings.ContainsKey(AEWS.UserSettingName.GroupingInformation)
                            ? response.Settings[AEWS.UserSettingName.GroupingInformation].ToString()
                            : "";

                    var extEwsUrl = response.Settings.ContainsKey(AEWS.UserSettingName.ExternalEwsUrl)
                            ? response.Settings[AEWS.UserSettingName.ExternalEwsUrl].ToString()
                            : "";

                   Logger.LogDebug(LoggingEvents.DebugEvent.MailAccountGroupAffinityFound, emailAddress, grpInfo, extEwsUrl);

                   return new MailGroupAffinity(emailAddress, grpInfo, extEwsUrl);
                }
                else
                {
                    Logger.LogDebug(LoggingEvents.DebugEvent.MailAccountGroupAffinityNotFound, emailAddress, response.ErrorCode, response.ErrorMessage);

                    return new MailGroupAffinity(emailAddress,
                        LoggingEvents.ErrorEvent.ExchangeGroupInformationNotFoundError(emailAddress, response.ErrorMessage, response.ErrorCode.ToString()), 
                        response.ErrorCode,
                        response.ErrorMessage);
                }
            }
            catch (ExchangeBaseException ex)
            {
                Logger.LogError(ex, ex.Event, emailAddress);
                return new MailGroupAffinity(emailAddress, ex.Event, ex.Message);
            }
            catch (EWS.ServiceResponseException ex)
            {
                Logger.LogError(ex, LoggingEvents.ErrorEvent.ServiceErrorGettingExchangeGroupInformation(emailAddress));
                return new MailGroupAffinity(emailAddress, LoggingEvents.ErrorEvent.ServiceErrorGettingExchangeGroupInformation(emailAddress), ex.Message);
            }
            catch (EWS.AutodiscoverLocalException ex)
            {
                Logger.LogError(ex, LoggingEvents.ErrorEvent.ExchangeAutodiscoverEndpointNotFoundError(emailAddress));
                return new MailGroupAffinity(emailAddress, LoggingEvents.ErrorEvent.ExchangeAutodiscoverEndpointNotFoundError(emailAddress), ex.Message);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, LoggingEvents.ErrorEvent.ErrorGettingExchangeGroupInformation(emailAddress));
                return new MailGroupAffinity(emailAddress, LoggingEvents.ErrorEvent.ErrorGettingExchangeGroupInformation(emailAddress), ex.Message);
            }
        }

        /// <summary>
        /// 
        /// <see cref="https://msdn.microsoft.com/en-us/library/microsoft.exchange.webservices.autodiscover.autodiscoverservice.getusersettings(v=exchg.80)"/>
        /// </summary>
        /// <param name="autoDiscoverService"></param>
        /// <param name="emailAddress"></param>
        /// <param name="enableScpLookup"></param>
        /// <param name="maxHops"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        private AEWS.GetUserSettingsResponse GetUserSettings(
            string emailAddress,
            int maxHops,
            params AEWS.UserSettingName[] settings)
        {
            Uri url = null;
            bool enableScpLookup = _autodiscoverService.EnableScpLookup;

            try
            {
                for (int attempt = 1; attempt <= maxHops; attempt++)
                {
                    _autodiscoverService.Url = url;
                    if (enableScpLookup)
                    {
                        _autodiscoverService.EnableScpLookup = (attempt < 3);
                    }

                    Logger.LogDebug(LoggingEvents.DebugEvent.DoAutodiscoverMailAccountWithParams(
                        attempt,
                        emailAddress,
                        _autodiscoverService.Url == null ? "(null)" : _autodiscoverService.Url.ToString(),
                        _autodiscoverService.EnableScpLookup));

                    AEWS.GetUserSettingsResponse response =
                        ExchangeServerUtils.ServerBusyRetry(
                            () => _autodiscoverService.GetUserSettings(emailAddress, settings),
                            "autoDiscoverService.GetUserSettings");

                    if (response.ErrorCode == AEWS.AutodiscoverErrorCode.RedirectAddress)
                    {
                        url = new Uri(response.RedirectTarget);
                        Logger.LogDebug(LoggingEvents.DebugEvent.AutodiscoverMailAccountUseTheUrl(emailAddress,response.RedirectTarget));
                    }
                    else if (response.ErrorCode == AEWS.AutodiscoverErrorCode.RedirectUrl)
                    {
                        url = new Uri(response.RedirectTarget);
                        Logger.LogDebug(LoggingEvents.DebugEvent.AutodiscoverMailAccountUseTheUrl(emailAddress,response.RedirectTarget));
                    }
                    else
                    {
                        if (response.ErrorCode == AEWS.AutodiscoverErrorCode.NoError)
                        {
                            Logger.LogDebug(LoggingEvents.DebugEvent.AutodiscoverMailAccountSuccessed(emailAddress));
                        }
                        else
                        {
                            Logger.LogDebug(LoggingEvents.DebugEvent.AutodiscoverMailAccountFailed(emailAddress,response.ErrorCode.ToString(), response.ErrorMessage));
                        }

                        return response;
                    }
                }
            }
            finally
            {
                // Restore the EnableScpLookup value
                _autodiscoverService.EnableScpLookup = enableScpLookup;
            }

            Logger.LogDebug(LoggingEvents.DebugEvent.AutodiscoverNoEndpointFound, emailAddress);

            throw new ExchangeSubscriptionGroupException(LoggingEvents.ErrorEvent.ExchangeAutodiscoverEndpointNotFoundError(emailAddress));
        }

        #region dublicated code from CreateExchangeServiceConnection

        //private static void LogExchangeConnectionConfiguration(IExchangeConnectionConfig exchangeConnectionConfig)
        //{
        //    Logger.LogDebug(LoggingEvents.DebugEvent.ExchangeConnectionConfigSetting(
        //        exchangeConnectionConfig.Version,
        //        exchangeConnectionConfig.UseDefaultCredentials,
        //        exchangeConnectionConfig.ServerUserEmailAccount,
        //        GetPasswordPrintout(exchangeConnectionConfig.Password),
        //        exchangeConnectionConfig.AutodiscoverUrl,
        //        exchangeConnectionConfig.EnableScpLookup,
        //        exchangeConnectionConfig.EwsApiTraceFlags));
        //}

        private static void SetupLogging(EWS.ExchangeServiceBase exchangeService, EWS.TraceFlags traceFlags, EWS.ITraceListener listener)
        {
           // Logger.LogDebug(LoggingEvents.DebugEvent.EwsTraceFlagSetting, traceFlags);

            if (traceFlags != EWS.TraceFlags.None)
            {
                exchangeService.TraceListener = listener ?? new LogTraceListener();
                exchangeService.TraceFlags = traceFlags;
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

        #endregion dublicated code from CreateExchangeServiceConnection

    }
}
