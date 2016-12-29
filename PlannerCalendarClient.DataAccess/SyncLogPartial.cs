using System.Linq;
using System.Collections.Generic;

namespace PlannerCalendarClient.DataAccess
{
    public partial class SyncLog
    {
        // All the error-codes can be found at this URL:
        // http://starwswiki.amstest.dk/CalendarEventReceiptErrorCodeType.ashx

        // These are fatal error codes that qualifies a SyncLog-item NOT to be re-send to Planner
        private static readonly int[] FatalErrorCodes = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 };
        // 6902,6903,6904,6905 = SOAP and timeout exception
        private static readonly int[] NonFatalErrorCodes = new int[] { 15, 16, 6902, 6903, 6904, 6905 };

        public SyncLog CopyToNew(string newStatus)
        {
            return new SyncLog
            {
                CalendarEnd = CalendarEnd,
                CalendarEvent = CalendarEvent,
                CalendarEventId = CalendarEventId,
                CalendarStart = CalendarStart,
                CreatedDate = CreatedDate,
                Operation = newStatus
            };
        }

        public bool IsPlannerOriginated
        {
            get
            {
                return PlannerSyncSuccess.HasValue &&
                       !PlannerSyncSuccess.Value && 
                       PlannerEventErrorCode.HasValue &&
                       PlannerEventErrorCode.Value.Equals(13);
            }
        }

        public bool CreateWasUnsuccessfulItemAlreadyExists
        {
            get
            {
                return Operation.Equals(Constants.SyncLogOperationCREATE) &&
                       PlannerSyncSuccess.HasValue &&
                       !PlannerSyncSuccess.Value &&
                       PlannerEventErrorCode.HasValue &&
                       PlannerEventErrorCode.Value.Equals(8);
            }
        }

        public bool UpdateWasUnsuccessfulItemNotInPlanner
        {
            get
            {
                return Operation.Equals(Constants.SyncLogOperationUPDATE) &&
                       PlannerSyncSuccess.HasValue &&
                       !PlannerSyncSuccess.Value &&
                       PlannerEventErrorCode.HasValue &&
                       PlannerEventErrorCode.Value.Equals(10);
            }
        }

        public bool FatalEventErrorDoNotResend
        {
            get
            {
                return PlannerEventErrorCode.HasValue && FatalErrorCodes.Any(x => x.Equals(PlannerEventErrorCode.Value));
            }
        }

        public bool QualifiesForResend
        {
            get
            {
                return PlannerSyncSuccess.HasValue &&
                       !PlannerSyncSuccess.Value &&
                       PlannerEventErrorCode.HasValue &&
                       !IsPlannerOriginated &&
                       !FatalEventErrorDoNotResend &&
                       NonFatalErrorCodes.Any(x => x.Equals(PlannerEventErrorCode.Value));
            }
        }
    }
}
