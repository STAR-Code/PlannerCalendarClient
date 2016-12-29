using System;
using System.Text;
using PlannerCalendarClient.Utility;

namespace PlannerCalendarClient.ServiceDfdg
{
    /// <summary>
    /// This class acts as a data container for a history object that is generated
    /// everytime a call (with a CalendarEventItem) are made to the Planner webservice (ExternalCalendarService).
    /// </summary>
    public class SyncLogItem
    {
        /// <summary>
        /// Date and time of when the call was made
        /// </summary>
        public DateTime SyncDate { get; set; }
        
        /// <summary>
        /// The name for the service method called for the item.
        /// This value is also in the ServiceCallReference object. 
        /// </summary>
        public EOperationName OperationName { get; set; }
        
        /// <summary>
        /// Indication of whether the call for the Calendar event item was a successful or not.
        /// The External Calendar Service provider that call planner sets this value to true if the call went well the item and to false if not.
        /// (It is a derive value of the PlannerEventErrorCode (true if the error code is 0).
        /// </summary>
        public bool PlannerSyncSuccess { get; set; }

        /// <summary>
        /// Response information received back from the webservice
        /// for each CalendarEventItem
        /// </summary>
        public string PlannerSyncResponse { get; set; }
        
        /// <summary>
        /// The Event status error code returned by planner.
        /// </summary>
        public int PlannerEventErrorCode { get; set; }
        
        /// <summary>
        /// Show that the sync of the item has raise a calendar event conflict and a mail has been sent.
        /// </summary>
        public bool PlannerConflictNotificationSent { get; set; }
        
        /// <summary>
        /// Response information received back from the webservice - a
        /// unique reference to the specific call in the service (used
        /// as log-reference)
        /// </summary>
        public Guid ServiceCallReferenceId { get; set; }

        /// <summary>
        /// For logging the object
        /// </summary>
        /// <returns>The contents of the obejct as a string</returns>
        public override string ToString()
        {
            var output = new StringBuilder();
            output.AppendFormat("SyncDate=\"{0}\"", SyncDate.ToString(CommonSettings.TimestampFormat));
            output.AppendFormat(",OperationName=\"{0}\"", OperationName);
            output.AppendFormat(",SyncSuccess=\"{0}\"", PlannerSyncSuccess);
            output.AppendFormat(",EventErrorCode=\"{0}\"", PlannerEventErrorCode);
            output.AppendFormat(",SyncResponse=\"{0}\"", PlannerSyncResponse ?? "(null)");
            output.AppendFormat(",ConflictNotificationSent=\"{0}\"", PlannerConflictNotificationSent);
            output.AppendFormat(",ServiceCallReferenceId=\"{0}\"", ServiceCallReferenceId);
            return output.ToString();
        }
    }
}
