using System;
using System.Runtime.Serialization;
using PlannerCalendarClient.ExchangeServiceCreator;
using PlannerCalendarClient.Logging;

namespace PlannerCalendarClient.EventProcessorService
{
    [Serializable]
    public class ExchangeMailboxNotFoundException : ExchangeBaseException
    {
        public ExchangeMailboxNotFoundException(string ewsId, string serviceUser, Exception ex)
            : base(LoggingEvents.ErrorEvent.ErrorExchangeMailboxNotFound(ewsId, serviceUser), ex)
        {
        }
    }

    class ExchangeAppointmentCalIdEmptyException : ExchangeBaseException
    {
        public ExchangeAppointmentCalIdEmptyException(LoggingEvents.ErrorEvent errorEvent)
            : base(errorEvent)
        {
        }
    }
}
