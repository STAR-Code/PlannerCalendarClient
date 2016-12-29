using System;
using EWS = Microsoft.Exchange.WebServices.Data;

namespace PlannerCalendarClient.EventProcessorService
{
    internal class AppointmentEx : Appointment, IAppointmentEx
    {
        public string UniqueId { get; set; }
        public EWS.Recurrence Recurrence { get; set; }
        public EWS.LegacyFreeBusyStatus LegacyFreeBusyStatus { get; set; }
        public DateTime? ICalRecurrenceId { get; set; }
        public EWS.OccurrenceInfoCollection ModifiedOccurrences { get; set; }
        public EWS.DeletedOccurrenceInfoCollection DeletedOccurrences { get; set; }

        public EWS.WellKnownFolderName WellKnownFolderName { get; set; }
    }
}