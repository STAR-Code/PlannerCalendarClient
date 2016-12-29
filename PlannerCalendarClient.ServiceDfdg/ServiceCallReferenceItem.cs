using System;
using System.Text;
using PlannerCalendarClient.Utility;

namespace PlannerCalendarClient.ServiceDfdg
{
    /// <summary>
    /// This class acts as a data-object for every call made to the webservice (log-information)
    /// </summary>
    public class ServiceCallReferenceItem
    {
        /// <summary>
        /// Added to support Serializable... Please use the other constructor...
        /// </summary>
        public ServiceCallReferenceItem()
        {
            OperationName = EOperationName.None;
            CallStarted = DateTime.Now;
        }

        /// <summary>
        /// The provided ID for a specific call to Planner
        /// </summary>
        public Guid? ServiceCallResponseReferenceId { get; set; }

        /// <summary>
        /// Name of the operation performed
        /// </summary>
        public EOperationName OperationName { get; set; }

        /// <summary>
        /// Timestamp for when the call was started
        /// </summary>
        public DateTime CallStarted { get; set; }

        /// <summary>
        /// Timestamp for when the call ended
        /// </summary>
        public DateTime CallEnded { get; set; }

        /// <summary>
        /// Indication of whether call to Planner was successful
        /// </summary>
        public bool? Success { get; set; }

        /// <summary>
        /// The respons from the call to planner as a text.
        /// </summary>
        public string ResponsText { get; set; }

        public override string ToString()
        {
            var output = new StringBuilder();
            output.AppendFormat("CallStarted={0}", CallStarted.ToString(CommonSettings.FullDateTimeFormat));
            output.AppendFormat(",CallEnded={0}", CallEnded.ToString(CommonSettings.FullDateTimeFormat));
            output.AppendFormat(",OperationName={0}", OperationName);
            output.AppendFormat(",Success={0}", Success);
            output.AppendFormat(",ServiceCallResponseReferenceId={0}", ServiceCallResponseReferenceId.HasValue ? ServiceCallResponseReferenceId.Value.ToString() : "(null)");
            output.AppendFormat(",ResponsText={0}", ResponsText ?? "");
            return output.ToString();
        }
    }
}
