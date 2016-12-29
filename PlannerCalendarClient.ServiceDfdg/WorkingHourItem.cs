using System;
using System.Text;

namespace PlannerCalendarClient.ServiceDfdg
{
    /// <summary>
    /// This class contains data about the work-period for one working day for a resource
    /// </summary>
    public class WorkingHourItem
    {
        /// <summary>
        /// Represents the day of the week
        /// </summary>
        public DayOfWeek WeekDay { get; set; }
        /// <summary>
        /// Start of the work-period
        /// </summary>
        public TimeSpan StartTime { get; set; }
        /// <summary>
        /// End of the work-period
        /// </summary>
        public TimeSpan EndTime { get; set; }

        public override string ToString()
        {
            var output = new StringBuilder();
            output.AppendFormat("WeekDay={0}", WeekDay);
            output.AppendFormat(",StartTime={0}", StartTime);
            output.AppendFormat(",EndTime={0}", EndTime);
            return output.ToString();
        }
    }
}
