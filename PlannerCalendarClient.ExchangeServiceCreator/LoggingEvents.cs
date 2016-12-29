using System;
using Microsoft.Exchange.WebServices.Data;
using PlannerCalendarClient.Logging;

namespace PlannerCalendarClient.ExchangeServiceCreator
{
    internal class LoggingEvents
    {
        private const ushort RangeStart = (ushort)EventIdRangeStart.ExchangeServiceCreator;

        internal class ErrorEvent : ErrorEventIdBase
        {
            private ErrorEvent(ushort eventId, string message)
                : base(eventId, message)
            {}
            
            internal static ErrorEvent CallFailedNoRetry(string method, int count, int maxCount)
            {
                return new ErrorEvent(RangeStart + 902, string.Format("Failed call the method {0} {1} of {2} times. Will not retry again!", method, count, maxCount));
            }

            //internal static ErrorEvent ExchangeFolderItemNotFound(string mailAccount)
            //{
            //    return new ErrorEvent(RangeStart + 903, string.Format("Calendar folder is not found for mail account '{0}'", mailAccount));
            //}

            internal static ErrorEvent ExchangeAffinityMailAccountIsMissing()
            {
                return new ErrorEvent(RangeStart + 904, "Configuration error. The autodiscover affinity mail account is missing."); 
            }

            internal static ErrorEvent ErrorInConfigurationOfTheServiceUserCredentials()
            {
                return new ErrorEvent(RangeStart + 905, "Configuration error. The service user mail and password are missing.");
            }
        }

        internal class InfoEvent : InfoEventIdBase
        {
            private InfoEvent(ushort eventId, string message)
                : base(eventId, message)
            {
            }

            internal static InfoEvent ServiceShutdownInProgressEvent()
            {
                return new InfoEvent(RangeStart + 101, "The service/application has been force to shutdown. Stopping is in progress.");
            }
        }

        internal class WarningEvent : WarningEventIdBase
        {
            private WarningEvent(ushort eventId, string message)
                : base(eventId, message)
            {
            }

            internal static WarningEvent CallFailedDoRetry(string methodName, int count, int maxCount, int waitTime, int backoff)
            {
                return new WarningEvent(RangeStart + 201, string.Format("Failed calling the method {0} {1} of {2} times. Will retry again after a pause of {3} ms. (BackOffMilliseconds {4})", methodName, count, maxCount, waitTime, backoff));
            }

            internal static WarningEvent CallFailedRecallImpersonated(string methodName, string mailbox)
            {
                return new WarningEvent(RangeStart + 202, string.Format("Failed calling the method {0}. Calling in again impersonate to the mailbox \"{1}\"", methodName, mailbox));
            }

            internal static WarningEvent CallFailedWithMissingImpersonateRight(string methodName, string mailbox)
            {
                return new WarningEvent(RangeStart + 203, string.Format("Impersonated call failed with missing rights to impersonate for the method {0} and the mailbox \"{1}\"", methodName, mailbox));
            }
        }

        internal class DebugEvent : DebugEventIdBase
        {
            private DebugEvent(ushort eventId, string message)
                : base(eventId, message)
            {
            }

            //internal static DebugEvent General(string msg)
            //{
            //    return new DebugEvent(RangeStart + 501, msg??"(null)");
            //}

            internal static DebugEvent RedirectionAllow(string url)
            {
                return new DebugEvent(RangeStart + 502, string.Format("Allow redirection Url for \"{0}\"",url));
            }

            internal static DebugEvent RedirectionDisallow(string url)
            {
                return new DebugEvent(RangeStart + 503, string.Format("Disallow redirection Url for \"{0}\"",url)); 
            }

            internal static DebugEvent ConnectedToExchange()
            {
                return new DebugEvent(RangeStart + 504, "Connected to exchange service.");
            }

            internal static DebugEvent UseDefaultCredentials(string domain, string username)
            {
                return new DebugEvent(RangeStart + 505,  string.Format("Use default credentials - current user: {0}\\{1}", domain, username)); 
            }

            internal static DebugEvent UseWebCredentials(string username)
            {
                return new DebugEvent(RangeStart + 506,  string.Format("Use web credentials - current user {0}",username));
            }

            internal static DebugEvent AutoDiscoverStart(string mailAccount)
            {
                return new DebugEvent(RangeStart + 507, string.Format("Call exchange autodiscover - Using the mail account \"{0}\" to Autodiscover the EWS URL.", mailAccount)); 
            }

            internal static DebugEvent AutoDiscoverSetEwsUrl(string url)
            {
                return new DebugEvent(RangeStart + 508,  string.Format("Autodiscover complete. EWS url set: \"{0}\".", url));
            }

            internal static DebugEvent ConfigurationSetEwsUrl(string url)
            {
                return new DebugEvent(RangeStart + 509,  string.Format("EWS url set from configuration: {0}.", url));
            }

            //internal static DebugEvent CertificateValidationCallBack(string status)
            //{
            //    return new DebugEvent(RangeStart + 510,  string.Format("Call of CertificateValidationCallBack with SSL Policy Error status: {0}.", status));
            //}

            internal static DebugEvent CertificateValidationCallBackSucceed(string position)
            {
                return new DebugEvent(RangeStart + 511,  string.Format("Call of CertificateValidationCallBack succeed (position: {0}).", position)); 
            }

            internal static DebugEvent CertificateValidationCallBackFailed(string issure)
            {
                return new DebugEvent(RangeStart + 512, string.Format("Call of CertificateValidationCallBack wil fail the validation and throw an exception. Issure: '{0}'", issure));
            }

            internal static DebugEvent EwsTraceFlagSetting(TraceFlags traceFlags)
            {
                return new DebugEvent(RangeStart + 513,  string.Format("Ews Traceflag set to: {0}.",traceFlags));
            }

            internal static DebugEvent ExchangeConnectionConfigSetting(ExchangeVersion version, bool useDefaultCredentials, string serverUserEmailAccount, string password, Uri autodiscoverUrl, bool enableScpLookup, TraceFlags traceFlags, bool useImpersonation)
            {
                return new DebugEvent(RangeStart + 514, string.Format("ExchangeConnectionConfig - Version: \"{0}\", UseDefaultCredentials: \"{1}\", ServerUserEmailAccount: \"{2}\", Password: \"{3}\", AutodiscoverUrl: \"{4}\", EnableScpLookup {5}, EwsTraceFlags: \"{6}\", UseImpersonation {7}.", version, useDefaultCredentials, serverUserEmailAccount, password, autodiscoverUrl, enableScpLookup, traceFlags,useImpersonation));
            }

            internal static DebugEvent EwsTraceLog(string msg)
            {
                return new DebugEvent(RangeStart + 515, msg ?? "(null)");
            }

            internal static DebugEvent ServicePointManagerDefaultConnectionLimit(int servicePointManagerDefaultConnectionLimitValue)
            {
                return new DebugEvent(RangeStart + 516, string.Format("The servicePointManager.DefaultConnectionLimit value set to {0}", servicePointManagerDefaultConnectionLimitValue));
            }

            //internal static DebugEvent CallExchangeFolderBind(string place, string mailbox)
            //{
            //    return new DebugEvent(RangeStart + 517, string.Format("Call exchange Folder.Bind for mailbox \"{0}\" from {1}", mailbox, place));
            //}

            //internal static DebugEvent CallExchangeFolderBindImpersonated(string place, string mailbox)
            //{
            //    return new DebugEvent(RangeStart + 518, string.Format("Call exchange Folder.Bind impersonating mailbox \"{0}\" from {1}", mailbox, place));
            //}

            internal static DebugEvent ImpersonatedCallSuccessed(string methodName, string mailbox)
            {
                return new DebugEvent(RangeStart + 519, string.Format("Impersonated call successed for the method {0} and the mailbox \"{1}\"", methodName, mailbox));
            }
        }
    }
}