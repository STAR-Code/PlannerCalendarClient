using System;
using System.Configuration;
using System.Security;
using Microsoft.Exchange.WebServices.Data;

namespace PlannerCalendarClient.ExchangeServiceCreator
{
    /// <summary>
    /// The base configuration parameter to create a connection to exchange
    /// </summary>
    public interface IExchangeConnectionConfig
    {
        ExchangeVersion Version { get; }
        bool UseDefaultCredentials { get; }
        string ServerUserEmailAccount { get; }
        SecureString Password { get; }
        Uri AutodiscoverUrl { get; set; }
        bool EnableScpLookup { get; }
        // Microsoft.Exchange.WebServices.Data.TraceFlags
        TraceFlags EwsApiTraceFlags { get; }
        bool UseImpersonation { get; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class Config 
    {
        /// <summary>
        /// The exchange service config data
        /// </summary>
        public class ExchangeConnectionConfig : IExchangeConnectionConfig
        {
            public ExchangeConnectionConfig(Uri exchangeServiceUrl, bool enableScpLookup, bool useImpersonation, ExchangeVersion version, TraceFlags ewsTraceFlag = TraceFlags.None)
            {
                Version = version;
                UseDefaultCredentials = true;
                ServerUserEmailAccount = null;
                Password = null;
                AutodiscoverUrl = exchangeServiceUrl;
                EnableScpLookup = enableScpLookup;
                UseImpersonation = useImpersonation;
                EwsApiTraceFlags = ewsTraceFlag;
            }

            public ExchangeConnectionConfig(string serverUserEmail, Uri exchangeServiceUrl, bool enableScpLookup, bool useImpersonation, ExchangeVersion version, TraceFlags ewsTraceFlag = TraceFlags.None)
            {
                Version = version;
                UseDefaultCredentials = true;
                ServerUserEmailAccount = serverUserEmail;
                Password = null;
                AutodiscoverUrl = exchangeServiceUrl;
                UseImpersonation = useImpersonation;
                EnableScpLookup = enableScpLookup;
                EwsApiTraceFlags = ewsTraceFlag;
            }

            public ExchangeConnectionConfig(string serverUserEmail, SecureString password, Uri exchangeServiceUrl, bool enableScpLookup, bool useImpersonation, ExchangeVersion version, TraceFlags ewsTraceFlag = TraceFlags.None)
            {
                UseDefaultCredentials = false;
                ServerUserEmailAccount = serverUserEmail;
                Password = password;
                Version = version;
                AutodiscoverUrl = exchangeServiceUrl;
                EnableScpLookup = enableScpLookup;
                UseImpersonation = useImpersonation;
                EwsApiTraceFlags = ewsTraceFlag;
            }

            public ExchangeConnectionConfig(string serverUserEmail, string password, Uri exchangeServiceUrl, bool enableScpLookup, bool useImpersonation, ExchangeVersion version, TraceFlags ewsTraceFlag = TraceFlags.None)
                : this(serverUserEmail, String2SecureString(password), exchangeServiceUrl, enableScpLookup, useImpersonation, version, ewsTraceFlag)
            {
            }

            protected ExchangeConnectionConfig(IExchangeConnectionConfig config)
            {
                UseDefaultCredentials = config.UseDefaultCredentials;
                ServerUserEmailAccount = config.ServerUserEmailAccount;
                Password = config.Password;
                Version = config.Version;
                AutodiscoverUrl = config.AutodiscoverUrl;
                EnableScpLookup = config.EnableScpLookup;
                EwsApiTraceFlags = config.EwsApiTraceFlags;
                UseImpersonation = config.UseImpersonation;
            }

            public ExchangeVersion Version { get; private set; }

            public bool UseDefaultCredentials { get; private set;  }

            public string ServerUserEmailAccount { get; private set; }

            public SecureString Password { get; private set; }

            public Uri AutodiscoverUrl { get; set; }

            public bool EnableScpLookup { get; private set;  }

            // Microsoft.Exchange.WebServices.Data.TraceFlags
            public TraceFlags EwsApiTraceFlags { get; private set; }

            public bool UseImpersonation { get; private set; }

            private static SecureString String2SecureString(string password)
            {
                var securePassword = new SecureString();

                foreach (var c in password)
                {
                    securePassword.AppendChar(c);
                }
                securePassword.MakeReadOnly();

                return securePassword;
            }
        }


        private static IExchangeConnectionConfig _exchangeConnectionConfig;

        /// <summary>
        /// Get the Exchange service config settings
        /// </summary>
        /// <returns>The exchange service config object</returns>
        public static IExchangeConnectionConfig GetConnectionConfig(string[] args)
        {
            return _exchangeConnectionConfig ?? (_exchangeConnectionConfig = RetrieveConfig(args));
        }

        public static ExchangeVersion InternalVersion
        {
            get { return ExchangeVersion.Exchange2013_SP1; }
        }

        /// <summary>
        /// 
        /// See the MSDN documentation <see cref="https://msdn.microsoft.com/en-us/library/system.net.servicepointmanager.defaultpersistentconnectionlimit(v=vs.110).aspx"/>
        /// </summary>
        public static int ServicePointManagerDefaultConnectionLimit
        {
            get { return Properties.Settings.Default.ServicePointManagerDefaultConnectionLimit; }
        }

        /// <summary>
        /// Read the exchange service config object properties from the command line 
        /// </summary>
        /// <returns>The exchange service config object</returns>
        protected static IExchangeConnectionConfig RetrieveConfig(string[] args)
        {
            var settings = Properties.Settings.Default;

            bool exchangeUseDefaultCredentials = settings.ExchangeUseDefaultCredentials;
            var email = String.IsNullOrWhiteSpace(settings.ExchangeServiceUserMail) ? null : settings.ExchangeServiceUserMail;
            var password = String.IsNullOrWhiteSpace(settings.ExchangeServiceUserPassword) ? null : settings.ExchangeServiceUserPassword;
            var exchangeServiceUrl = String.IsNullOrWhiteSpace(settings.exchangeServiceUrl) ? null : new Uri(settings.exchangeServiceUrl);
            var enableScpLookup = settings.EnableScpLookup;
            var ewsTraceFlags = settings.EwsTraceFlags;
            var useImpersonation = settings.UseImpersonation;

            // the cmd args
            const string defaultCredentialsPrefix = "/defaultCredentials:";
            const string emailPrefix = "/serverUserEmail:";
            const string passwordPrefix = "/password:";
            const string exchangeServiceUrlPrefix = "/ExchangeServiceUrl:";
            const string enableScpLookupPrefix = "/EnableScpLookup:";
            const string ewsTraceFlagsPrefix = "/EwsTraceFlags:";
            const string useImpersonationPerfix = "/UseImpersonation:";

            if (args != null)
            {
                foreach (var arg in args)
                {
                    if (arg.StartsWith("/"))
                    {
                        if (arg.StartsWith(emailPrefix, StringComparison.InvariantCultureIgnoreCase))
                        {
                            email = arg.Substring(emailPrefix.Length);
                        }
                        else if (arg.StartsWith(passwordPrefix, StringComparison.InvariantCultureIgnoreCase))
                        {
                            password = arg.Substring(passwordPrefix.Length);
                        }
                        else if (arg.StartsWith(defaultCredentialsPrefix, StringComparison.InvariantCultureIgnoreCase))
                        {
                            exchangeUseDefaultCredentials = ParseYesNoArgument(defaultCredentialsPrefix, arg);
                        }
                        else if (arg.StartsWith(exchangeServiceUrlPrefix, StringComparison.InvariantCultureIgnoreCase))
                        {
                            exchangeServiceUrl = new Uri(arg.Substring(exchangeServiceUrlPrefix.Length));
                        }
                        else if (arg.StartsWith(enableScpLookupPrefix, StringComparison.InvariantCultureIgnoreCase))
                        {
                            enableScpLookup = ParseYesNoArgument(exchangeServiceUrlPrefix, arg);
                        }
                        else if (arg.StartsWith(useImpersonationPerfix, StringComparison.InvariantCultureIgnoreCase))
                        {
                            useImpersonation = ParseYesNoArgument(useImpersonationPerfix, arg);
                        }
                        else if (arg.StartsWith(ewsTraceFlagsPrefix, StringComparison.InvariantCultureIgnoreCase))
                        {
                            ewsTraceFlags = ParseTraceFlags(ewsTraceFlagsPrefix, arg);
                        }
                        else
                        {
                            throw new Exception(string.Format("Unknown argument {0}.", arg));
                        }
                    }
                }
            }

            if (exchangeUseDefaultCredentials)
                return new ExchangeConnectionConfig(email, exchangeServiceUrl, enableScpLookup, useImpersonation, InternalVersion, ewsTraceFlags);
            
            if (email != null && password != null)
                return new ExchangeConnectionConfig(email, password, exchangeServiceUrl, enableScpLookup, useImpersonation, InternalVersion, ewsTraceFlags);
            
            throw new ExchangeCreateServiceException(LoggingEvents.ErrorEvent.ErrorInConfigurationOfTheServiceUserCredentials());
        }

        protected static bool ParseYesNoArgument(string prefixString, string arg)
        {
            var argValue = arg.Substring(prefixString.Length);

            if (argValue.Equals("yes", StringComparison.CurrentCultureIgnoreCase))
            {
                return true;
            }
            
            if (argValue.Equals("no", StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }

            // commandline option value is not recognised
            throw new Exception(string.Format("Unknown argument ({0}) value: {1}.", prefixString, argValue));
        }

        protected static TraceFlags ParseTraceFlags(string prefixString, string arg)
        {
            TraceFlags ewsTraceFlags;

            var argValue = arg.Substring(prefixString.Length);
            if (Enum.TryParse(argValue, true, out ewsTraceFlags))
            {
                return ewsTraceFlags;
            }

            // commandline option value is not recognised
            throw new Exception(string.Format("Unknown argument ({0}) value: {1}.", prefixString, argValue));
        }
    }
}
