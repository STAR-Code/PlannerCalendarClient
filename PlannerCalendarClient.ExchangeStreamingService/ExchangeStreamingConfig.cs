using System;
using PlannerCalendarClient.ExchangeServiceCreator;

namespace PlannerCalendarClient.ExchangeStreamingService
{
    public interface IExchangeStreamingConfig : IExchangeConnectionConfig
    {
        int ConnectionTimeout { get; }
        TimeSpan SubscriberMailUpdateTimeInterval { get; }
        bool DeactivateSolvingOfGroupAffinity { get; }

        #region Subscription tree properies

        int MaxSubscriptionPerConnection { get; }
        int MaxSubscriptionsPerSubscriptionGroup { get; }

        #endregion Subscription tree properies
    }

    public class Config : ExchangeServiceCreator.Config
    {

        /// <summary>
        /// The exchange streaming configuration data
        /// </summary>
        public class ExchangeStreamingConfig : ExchangeConnectionConfig, IExchangeStreamingConfig
        {
            public ExchangeStreamingConfig(
                IExchangeConnectionConfig config, 
                int connectionTimeout, 
                TimeSpan subscriberMailUpdateTimeInterval,
                bool deactivateSolvingOfGroupAffinity,
                int maxSubscriptionPerConnection,
                int maxSubscriptionsPerSubscriptionGroup)
                : base(config)
            {
                ConnectionTimeout = connectionTimeout;
                SubscriberMailUpdateTimeInterval = subscriberMailUpdateTimeInterval;
                DeactivateSolvingOfGroupAffinity = deactivateSolvingOfGroupAffinity;
                MaxSubscriptionPerConnection = maxSubscriptionPerConnection;
                MaxSubscriptionsPerSubscriptionGroup = maxSubscriptionsPerSubscriptionGroup;
            }

            protected ExchangeStreamingConfig(IExchangeStreamingConfig exchangeStreamingConfig)
                : base(exchangeStreamingConfig)
            {
                ConnectionTimeout = exchangeStreamingConfig.ConnectionTimeout;
                SubscriberMailUpdateTimeInterval = exchangeStreamingConfig.SubscriberMailUpdateTimeInterval;
                DeactivateSolvingOfGroupAffinity = exchangeStreamingConfig.DeactivateSolvingOfGroupAffinity;
                MaxSubscriptionPerConnection = exchangeStreamingConfig.MaxSubscriptionPerConnection;
                MaxSubscriptionsPerSubscriptionGroup = exchangeStreamingConfig.MaxSubscriptionsPerSubscriptionGroup;
            }

            public int ConnectionTimeout { get; private set; }

            public TimeSpan SubscriberMailUpdateTimeInterval { get; private set; }

            public bool DeactivateSolvingOfGroupAffinity { get; private set; }

            public int MaxSubscriptionPerConnection { get; private set; }

            public int MaxSubscriptionsPerSubscriptionGroup { get; private set; }
        }

        private static IExchangeStreamingConfig _exchangeStreamingConfig;

        /// <summary>
        /// Get the Exchange configuration
        /// </summary>
        /// <returns>The exchange configuration object</returns>
        public static IExchangeStreamingConfig GetExchangeConfigData(string[] args)
        {
            if (_exchangeStreamingConfig == null)
            {
                _exchangeStreamingConfig = RetrieveExchangeConfigData(args);
            }

            return _exchangeStreamingConfig;
        }

        /// <summary>
        /// Get the Exchange configuration by merging the application configuration and commandline options
        /// </summary>
        /// <returns>The exchange streaming service configuration object</returns>
        private static IExchangeStreamingConfig RetrieveExchangeConfigData(string[] args)
        {
            var settings = Properties.Settings.Default;

            int connectTimeout = settings.ExchangeConnectionTimeout;
            var subscriberUpdateTimeInterval = settings.SubscriberUpdateTimeInterval;
            var deactivateSolvingOfGroupAffinity = settings.DeactivateSolvingOfGroupAffinity;
            int maxSubscriptionPerConnection = settings.MaxSubscriptionPerConnection;
            int maxSubscriptionsPerSubscriptionGroup = settings.MaxSubscriptionsPerSubscriptionGroup;

            IExchangeConnectionConfig exchangeConnectionConfig = ExchangeServiceCreator.Config.GetConnectionConfig(args);

            const string connectTimeoutPrefix = "/ConnectionTimeout:";
            const string subscriberUpdateTimeIntervalPrefix = "/SubscriberUpdateTimeInterval:";
            const string deactivateSolvingOfGroupAffinityPrefix = "/DeactivateSolvingOfGroupAffinity:";
            const string maxSubscriptionPerConnectionPrefix = "/MaxSubscriptionPerConnection:";
            const string maxSubscriptionsPerSubscriptionGroupPrefix = "/MaxSubscriptionsPerSubscriptionGroup:";

            if (args != null)
            {
                foreach (var arg in args)
                {
                    if (arg.StartsWith("/"))
                    {
                        if (arg.StartsWith(deactivateSolvingOfGroupAffinityPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            deactivateSolvingOfGroupAffinity = ParseYesNoArgument(
                                deactivateSolvingOfGroupAffinityPrefix, arg);
                        }
                        else if (arg.StartsWith(connectTimeoutPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            string connectionTimeValue = arg.Substring(connectTimeoutPrefix.Length);
                            if (!int.TryParse(connectionTimeValue, out connectTimeout))
                            {
                                // commandline option value is not reconnice
                            }
                        }
                        else if (arg.StartsWith(subscriberUpdateTimeIntervalPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            string subscriberUpdateTimeIntervalValue =
                                arg.Substring(subscriberUpdateTimeIntervalPrefix.Length);
                            if (!TimeSpan.TryParse(subscriberUpdateTimeIntervalValue, out subscriberUpdateTimeInterval))
                            {
                                // commandline option value is not reconnice
                            }
                        }
                        else if (arg.StartsWith(maxSubscriptionPerConnectionPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            string maxSubscriptionPerConnectionValue =
                                arg.Substring(maxSubscriptionPerConnectionPrefix.Length);
                            if (!int.TryParse(maxSubscriptionPerConnectionValue, out maxSubscriptionPerConnection))
                            {
                                // commandline option value is not reconnice
                            }
                        }
                        else if (arg.StartsWith(maxSubscriptionsPerSubscriptionGroupPrefix,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            string maxSubscriptionsPerSubscriptionGroupValue =
                                arg.Substring(maxSubscriptionsPerSubscriptionGroupPrefix.Length);
                            if (
                                !int.TryParse(maxSubscriptionsPerSubscriptionGroupValue,
                                    out maxSubscriptionsPerSubscriptionGroup))
                            {
                                // commandline option value is not reconnice
                            }
                        }
                        else
                        {
                            // commandline option is not recognise
                        }
                    }
                }
            }

            return new ExchangeStreamingConfig(
                exchangeConnectionConfig, 
                connectTimeout, 
                subscriberUpdateTimeInterval, 
                deactivateSolvingOfGroupAffinity, 
                maxSubscriptionPerConnection, 
                maxSubscriptionsPerSubscriptionGroup);
        }
    }
}
