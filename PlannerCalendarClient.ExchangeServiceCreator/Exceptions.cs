using System;
using PlannerCalendarClient.Logging;

namespace PlannerCalendarClient.ExchangeServiceCreator
{
    /// <summary>
    /// Base exception class for this modules
    /// </summary>
    public abstract class ExchangeBaseException : LoggingBaseException
    {
        protected ExchangeBaseException(EventIdBase errorEvent, params object[] args) :
            base(errorEvent, args)
        { }

        protected ExchangeBaseException(EventIdBase errorEvent, Exception ex, params object[] args) :
            base(errorEvent, ex, args)
        { }
    }


    /// <summary>
    /// 
    /// </summary>
    class ExchangeCreateServiceException : ExchangeBaseException
    {
        public ExchangeCreateServiceException(LoggingEvents.ErrorEvent errorEvent, params object[] args) :
            base(errorEvent, args)
        { }
    }

    /// <summary>
    /// Throw this exception when the application is trying to stop the flow and shutdown the service/application 
    /// </summary>
    class ShutdownInProgressException : ExchangeBaseException
    {
        public ShutdownInProgressException() :
            base(LoggingEvents.InfoEvent.ServiceShutdownInProgressEvent())
        { }
    }

    /// <summary>
    /// Throw this exception when the application is trying to stop the flow and shutdown the service/application 
    /// </summary>
    class  CertificateValidationException : ExchangeBaseException
    {
        public CertificateValidationException(string url) :
            base(LoggingEvents.DebugEvent.CertificateValidationCallBackFailed(url))
        { }
    }
}
