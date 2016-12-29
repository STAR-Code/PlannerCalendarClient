using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlannerCalendarClient.DataAccess;
using PlannerCalendarClient.PlannerCommunicatorService;

namespace PlannerCalendarClient.UnitTest.PlannerCommunicatorService
{
    [TestClass]
    public class TestCalendarHelper
    {
        [TestMethod]
        public void Test_TryGetLatest_SyncLogExists()
        {
            const int latestEventId = 666;

            var calendarEvent = new CalendarEvent();
            calendarEvent.SyncLogs.AddRange(GetTestEvents());
            calendarEvent.SyncLogs.Add(new SyncLog() { CreatedDate = DateTime.Now, CalendarEventId = latestEventId });
            calendarEvent.SyncLogs.AddRange(GetTestEvents());

            SyncLog syncLog;
            var result = calendarEvent.TryGetLatestSyncLog(out syncLog);

            Assert.IsTrue(result);
            Assert.AreEqual(latestEventId, syncLog.CalendarEventId);
        }

        [TestMethod]
        public void Test_TryGetLatestSyncLog_NoSyncLog()
        {
            var calendarEvent = new CalendarEvent();
            
            SyncLog syncLog;
            var result = calendarEvent.TryGetLatestSyncLog(out syncLog);

            Assert.IsFalse(result);
            Assert.IsNull(syncLog);
        }

        private IEnumerable<SyncLog> GetTestEvents(int noOfEventsToCreate = 20, string operation = "C")
        {
            var e = new List<SyncLog>();
            int i = 0;
            while (i < noOfEventsToCreate)
            {
                i++;

                e.Add(new SyncLog
                {
                    CreatedDate = DateTime.Now.AddDays(-i),
                    CalendarEvent = null,
                    CalendarEventId = i, // Unique id
                    CalendarStart = DateTime.Now,
                    CalendarEnd = DateTime.Now,
                    Operation = operation,

                });

            }
            var events = new EnumerableQuery<SyncLog>(e);

            return events;
        }
    }

    public static class CollectionHelpers
    {
        public static void AddRange<T>(this ICollection<T> destination,
                                       IEnumerable<T> source)
        {
            foreach (T item in source)
            {
                destination.Add(item);
            }
        }
    }
}
