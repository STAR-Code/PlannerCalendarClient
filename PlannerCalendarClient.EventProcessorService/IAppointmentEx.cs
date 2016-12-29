using System;
using EWS = Microsoft.Exchange.WebServices.Data;

namespace PlannerCalendarClient.EventProcessorService
{
    interface IAppointmentEx : IAppointment
    {
        /// <summary>
        /// Exchange appointment unique Id
        /// </summary>
        string UniqueId { get; }

        EWS.Recurrence Recurrence { get; }

        EWS.LegacyFreeBusyStatus LegacyFreeBusyStatus { get; }

        DateTime? ICalRecurrenceId { get; }

        EWS.WellKnownFolderName WellKnownFolderName { get; }

        EWS.OccurrenceInfoCollection ModifiedOccurrences { get; set; }
        EWS.DeletedOccurrenceInfoCollection DeletedOccurrences { get; set; }
    }
}