using System;
using Microsoft.Exchange.WebServices.Data;
using PlannerCalendarClient.ExchangeServiceCreator;

namespace PlannerCalendarClient.ExchangeStreamingService
{
    class ExchangeStreamConnectionException : ExchangeBaseException
    {
        public ExchangeStreamConnectionException(LoggingEvents.ErrorEvent errorEvent, params object[] args) :
            base(errorEvent, args)
        { }

        public ExchangeStreamConnectionException(LoggingEvents.ErrorEvent errorEvent, Exception ex, params object[] args) :
            base(errorEvent, ex, args)
        { }

        public ExchangeStreamConnectionException(LoggingEvents.WarningEvent warningEvent, params object[] args) :
            base(warningEvent, args)
        { }
    }

    /// <summary>
    /// 
    /// </summary>
    class ExchangeSubscriptionGroupException : ExchangeBaseException
    {
        public ExchangeSubscriptionGroupException(LoggingEvents.ErrorEvent errorEvent) :
            base(errorEvent)
        { }

        public ExchangeSubscriptionGroupException(LoggingEvents.ErrorEvent errorEvent, params object[] args) :
            base(errorEvent, args)
        { }

        public ExchangeSubscriptionGroupException(LoggingEvents.ErrorEvent errorEvent, ServiceResponseException ex, params object[] args) :
            base(errorEvent, ex, args)
        {
        }

        public ExchangeSubscriptionGroupException(LoggingEvents.ErrorEvent errorEvent, Exception ex, params object[] args) :
            base(errorEvent, ex, args)
        { }

        public ExchangeSubscriptionGroupException(LoggingEvents.WarningEvent warningEvent) :
            base(warningEvent)
        { }

        public ExchangeSubscriptionGroupException(LoggingEvents.WarningEvent warningEvent, params object[] args) :
            base(warningEvent, args)
        { }

        //public AutodiscoverErrorCode AutodiscoverErrorCode { get; private set; }
        //public string AutodiscoverErrorMessage { get; private set; }
        
    }
}