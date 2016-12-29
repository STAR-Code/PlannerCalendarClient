using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlannerCalendarClient.Logging
{
    /// <summary>
    /// Abstract base exception class for the logging module.
    /// </summary>
    public abstract class LoggingBaseException : Exception
    {
        protected LoggingBaseException(EventIdBase errorEvent, params object[] args) :
            base(FormatMessage(errorEvent, args))
        {
            Event = errorEvent;
        }

        protected LoggingBaseException(EventIdBase errorEvent, Exception ex, params object[] args) :
            base(FormatMessage(errorEvent, args), ex)
        {
            Event = errorEvent;
        }

        public EventIdBase Event { get; private set; }

        private static string FormatMessage(EventIdBase errorEvent, params object[] args)
        {
            var formatText = errorEvent.ToString() + " " + errorEvent.Message;

            string msg = formatText.SafeFormat(args);

            return msg;
        }
    }

}
