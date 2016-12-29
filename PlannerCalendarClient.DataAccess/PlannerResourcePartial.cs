using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlannerCalendarClient.DataAccess
{
    public partial class PlannerResource
    {
        public bool IsQualifiedForSynchronization
        {
            get
            {
                return !string.IsNullOrEmpty(MailAddress) &&
                       !DeletedDate.HasValue &&
                       !string.IsNullOrEmpty(GroupAffinity) &&
                       string.IsNullOrEmpty(ErrorCode);
            }
        }
    }
}
