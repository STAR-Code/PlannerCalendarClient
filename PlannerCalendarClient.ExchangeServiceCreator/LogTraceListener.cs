using Microsoft.Exchange.WebServices.Data;
using PlannerCalendarClient.Logging;

namespace PlannerCalendarClient.ExchangeServiceCreator
{
    /// <summary>
    /// TraceListener adaptor for the ews library that forwards the logging events to the application logging framework.
    /// </summary>
    public class LogTraceListener : ITraceListener
    {
        public void Trace(string traceType, string traceMessage)
        {
            var logger = Logger.GetLogger(traceType);
            logger.LogDebug(LoggingEvents.DebugEvent.EwsTraceLog(traceMessage));
        }
    }
}