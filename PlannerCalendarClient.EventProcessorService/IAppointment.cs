using System;

namespace PlannerCalendarClient.EventProcessorService
{
    public interface IAppointment
    {
        DateTime Start { get; set; }
        DateTime End { get; set; }
        string EmailAddress { get; set; }
        bool IsCancelled { get; set; }
        bool IsDeleted { get; set; }
        string ICalUid { get; set; }
        bool IsRecurring { get; set; }
        bool IsFree { get; set; }
        string ToString();
    }
}
