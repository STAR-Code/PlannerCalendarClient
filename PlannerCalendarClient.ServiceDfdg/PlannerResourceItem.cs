using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace PlannerCalendarClient.ServiceDfdg
{
    /// <summary>
    /// This class represents a caseworker or a room in Planner (known as a Resource)
    /// </summary>
    public class ResourceItem
    {
        /// <summary>
        /// The Planner ID of the resource (ResourceID in the planner database)
        /// </summary>
        public Guid? Id { get; set; }
        
        /// <summary>
        /// The name of the resource (Name(50) in the planner database)
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// An external identifier for the resource (could be RID) (ExternalIdentifier(65) in the planner database)
        /// </summary>
        public string ExternalId { get; set; }
        
        /// <summary>
        /// The mailaddress for the resource (string max length 254 or 256 charaters (RFC 5321))
        /// </summary>
        public string MailAddress { get; set; }
        
        /// <summary>
        /// Some description of the resource
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Indication of whether the resource is a caseworker or a room
        /// </summary>
        public bool IsCaseWorker { get; set; }

        /// <summary>
        /// Information about working hours setup in Planner (monday to friday)
        /// </summary>
        public List<WorkingHourItem> WorkingDayInfo { get; set; }

        public override string ToString()
        {
            var output = new StringBuilder();
            output.AppendFormat("Id={0}", Id.HasValue ? Id.Value.ToString() : "");
            output.AppendFormat(",Name={0}", Name);
            output.AppendFormat(",ExternalId={0}", ExternalId);
            output.AppendFormat(",MailAddress={0}", MailAddress);
            output.AppendFormat(",Description={0}", Description);
            output.AppendFormat(",IsCaseWorker={0}", IsCaseWorker);
            foreach (var item in WorkingDayInfo)
            {
                output.AppendFormat(",WorkingHourItem={0}", item);
            }
            return output.ToString();
        }
    }
}
