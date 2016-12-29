//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace PlannerCalendarClient.DataAccess
{
    using System;
    using System.Collections.Generic;
    
    public partial class ServiceCallReferenceLog
    {
        public ServiceCallReferenceLog()
        {
            this.SyncLogs = new HashSet<SyncLog>();
        }
    
        public long Id { get; set; }
        public Nullable<System.Guid> ServiceCallResponseReferenceId { get; set; }
        public string Operation { get; set; }
        public System.DateTime CallStarted { get; set; }
        public Nullable<System.DateTime> CallEnded { get; set; }
        public Nullable<bool> Success { get; set; }
        public string ResponseText { get; set; }
    
        public virtual ICollection<SyncLog> SyncLogs { get; set; }
    }
}