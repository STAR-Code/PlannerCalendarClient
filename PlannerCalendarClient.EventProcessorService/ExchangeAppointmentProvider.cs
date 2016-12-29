using System;
using System.Collections.Generic;
using System.Linq;
using EWS = Microsoft.Exchange.WebServices.Data;

namespace PlannerCalendarClient.EventProcessorService
{
    class ExchangeAppointmentProvider : IAppointmentProvider
    {
        private static readonly Logging.ILogger Logger = Logging.Logger.GetLogger();

        private readonly ExchangeRecurrenceAppointmentSolver _exchangeRecurrenceAppointmentSolver;
        private readonly IExchangeGateway _exchangeGateway;
        private readonly int _maxParallelism;

        public ExchangeAppointmentProvider(IExchangeGateway exchangeGateway, ExchangeRecurrenceAppointmentSolver exchangeRecurrenceAppointmentSolver, int maxParallelism)
        {
            _exchangeGateway = exchangeGateway;
            _exchangeRecurrenceAppointmentSolver = exchangeRecurrenceAppointmentSolver;
            _maxParallelism = maxParallelism;
        }

        public IEnumerable<IAppointment> GetAppointmentsById(string ewsId, DateTime start, DateTime end)
        {
            IAppointmentEx appointment = _exchangeGateway.GetAppointment(ewsId);

            IEnumerable<IAppointment> result;
            if (!appointment.IsRecurring)
            {
                result = new IAppointmentEx[] { appointment };
            }
            else
            {
                try
                {
                    Logger.LogDebug(LoggingEvents.DebugEvent.IsMasterAppointment(appointment.UniqueId));
                    result = _exchangeRecurrenceAppointmentSolver.UnfoldRecurrenceAppoints(appointment, start, end);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, LoggingEvents.ErrorEvent.AppointmentOccurresUnfoldError(appointment.EmailAddress));
                    throw;
                }
            }

            return result;
        }

        public IEnumerable<IAppointment> GetAppointmentsByMailbox(string mailBox, DateTime startDate, DateTime endDate)
        {
            if (string.IsNullOrWhiteSpace(mailBox)) throw new ArgumentNullException("mailbox");

            var appointmentsX = _exchangeGateway.GetAppointmentFromExchangeCalendarView(mailBox, startDate, endDate);

            var appointments = appointmentsX
                .Where(
                    t =>
                        t.LegacyFreeBusyStatus == EWS.LegacyFreeBusyStatus.Busy ||
                        t.LegacyFreeBusyStatus == EWS.LegacyFreeBusyStatus.Free ||
                        t.LegacyFreeBusyStatus == EWS.LegacyFreeBusyStatus.OOF ||
                        t.LegacyFreeBusyStatus == EWS.LegacyFreeBusyStatus.Tentative)
                .ToList();

            return appointments;
        }
    }
}
