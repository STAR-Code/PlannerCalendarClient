using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PlannerCalendarClient.Utility;

namespace PlannerCalendarClient.ServiceDfdg
{
    /// <summary>
    /// This class acts as a data container for the calendar events that will be
    /// sent (or was sent) to Planner.
    /// 
    /// The basic calendar properties for the CalendarEventItem are:
    ///     Start
    ///     End
    ///     IsDeleted
    ///     OriginId
    ///     OriginMailAddress
    ///     
    /// When one of these properies change the Modify property is opdated.
    /// 
    /// </summary>
    public class CalendarEventItem
    {
        public CalendarEventItem()
        {
            Description = string.Empty;
        }

        /// <summary>
        /// This property is a locale only. It is never sent to planner.
        /// It is mean to make it easier for the test user to reconize a specific calendar event item, by having
        /// the possiblitity to give it a name.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Startdate and time for the calendar event
        /// (posted to Planner)
        /// </summary>
        public DateTime Start { get; set; }
        
        /// <summary>
        /// Enddate and time for the calendar event
        /// (posted to Planner)
        /// </summary>
        public DateTime End { get; set; }
        
        /// <summary>
        /// Identification (mail) of the owner of the calendar event
        /// </summary>
        public string OriginMailAddress { get; set; }
        
        /// <summary>
        /// Unique identifier defined by the external calendar provider.
        /// This value should only be set once.
        /// </summary>
        public string OriginId { get; set; }
        
        /// <summary>
        /// This reflects the unique identifier of the calendar event
        /// that Planner returns after creating the event
        /// </summary>
        public Guid? PlannerCalendarEventId { get; set; }
        
        /// <summary>
        /// This reflects the unique identification of the resource
        /// in Planner that owns the calendar event (typically returned
        /// from Planner)
        /// </summary>
        public Guid? PlannerResourceId { get; set; }

        /// <summary>
        /// Return value with information about call made to Planner
        /// </summary>
        public SyncLogItem SyncLogItem { get; set; }

        /// <summary>
        /// Return indication of whether the item has been deleted in Planner
        /// </summary>
        public bool HasBeenDeleted { get; set; }

        /// <summary>
        /// For logging the object
        /// </summary>
        /// <returns>The contents of the obejct as a string</returns>
        public override string ToString()
        {
            var output = new StringBuilder();
            output.AppendFormat("Description={0}", Description ?? "(null)");
            output.AppendFormat(",Start={0}", Start.ToString(CommonSettings.FullDateTimeFormat));
            output.AppendFormat(",End={0}", End.ToString(CommonSettings.FullDateTimeFormat));
            output.AppendFormat(",OriginId={0}", OriginId);
            output.AppendFormat(",OriginMailAddress={0}", OriginMailAddress);
            output.AppendFormat(",PlannerCalendarEventId={0}", PlannerCalendarEventId.HasValue ? PlannerCalendarEventId.Value.ToString() : "(null)");
            output.AppendFormat(",PlannerResourceId={0}", PlannerResourceId.HasValue ? PlannerResourceId.Value.ToString() : "(null)");
            output.AppendFormat(",SyncLogItem=({0})", SyncLogItem == null ? "null" : SyncLogItem.ToString());
            return output.ToString();
        }
    }
}
