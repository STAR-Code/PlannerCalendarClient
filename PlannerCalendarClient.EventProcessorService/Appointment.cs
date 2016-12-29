using System;
using System.Text;

namespace PlannerCalendarClient.EventProcessorService
{
    public class Appointment : IAppointment
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string EmailAddress { get; set; }
        public bool IsCancelled { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsFree { get; set; }
        public string ICalUid { get; set; }
        public bool IsRecurring { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("EmailAddress = {0}; Start = {1}; End = {2}", EmailAddress, Start, End);
            if (IsDeleted)
                sb.Append("; IsDeleted");
            if (IsCancelled)
                sb.Append("; IsCancelled");
            if (IsFree)
                sb.Append("; IsFree");
            if (IsRecurring)
                sb.Append("; IsRecurring");
            return sb.ToString();
        }
    }
}
