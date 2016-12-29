using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlannerCalendarClient.DataAccess;
using PlannerCalendarClient.Logging;
using PlannerCalendarClient.PlannerCommunicatorService;
using PlannerCalendarClient.ServiceDfdg;

namespace PlannerCalendarClient.UnitTest.PlannerCommunicatorService
{
    [TestClass]
    public class TestCalendarEventSynchronizer
    {
        private ILogger _logger;

        [TestInitialize]
        public void SetupTest()
        {
            _logger = new TestLogger();
        }

        [TestMethod]
        public void Test_EventIsUpToDate()
        {
            var startTime = DateTime.Now;
            var endTime = DateTime.Now.AddHours(2);

            var syncLogs = new Collection<SyncLog>
            {
                new SyncLog {CalendarEnd = endTime, CalendarStart = startTime.AddHours(-1), CreatedDate = DateTime.Now.AddHours(-1), SyncDate = DateTime.Now.AddHours(-1)},
                new SyncLog {CalendarEnd = endTime, CalendarStart = startTime, CreatedDate = DateTime.Now, SyncDate = DateTime.Now} // Up-to-date
            };

            var calendarEvent = new CalendarEvent { IsDeleted = false, SyncLogs = syncLogs };
            var calendarEventItem = new CalendarEventItem { Start = startTime, End = endTime };

            var syncResult = CalendarEventSynchronizer.SynchronizeCalendarEvent(calendarEvent, calendarEventItem);

            Assert.AreEqual(CalendarEventSynchronizer.CalendarEventSyncResult.UpToDate, syncResult);
        }

        [TestMethod]
        public void Test_EventIsNotUpToDate()
        {
            var startTime = DateTime.Now;
            var endTime = DateTime.Now.AddHours(2);

            var syncLogs = new Collection<SyncLog> { new SyncLog { CalendarEnd = endTime, CalendarStart = startTime, SyncDate = DateTime.Now } };

            var calendarEvent = new CalendarEvent { IsDeleted = false, SyncLogs = syncLogs };
            var calendarEventItem = new CalendarEventItem { Start = startTime, End = endTime.AddHours(1) };

            var syncResult = CalendarEventSynchronizer.SynchronizeCalendarEvent(calendarEvent, calendarEventItem);

            Assert.AreEqual(CalendarEventSynchronizer.CalendarEventSyncResult.Updated, syncResult);
        }

        [TestMethod]
        public void Test_EventIsDeleted()
        {
            var startTime = DateTime.Now;
            var endTime = DateTime.Now.AddHours(2);

            var syncLogs = new Collection<SyncLog> { new SyncLog { CalendarEnd = endTime, CalendarStart = startTime, SyncDate = DateTime.Now } };

            var calendarEvent = new CalendarEvent { IsDeleted = true, SyncLogs = syncLogs };
            var calendarEventItem = new CalendarEventItem { Start = startTime, End = endTime };

            var syncResult = CalendarEventSynchronizer.SynchronizeCalendarEvent(calendarEvent, calendarEventItem);

            Assert.AreEqual(CalendarEventSynchronizer.CalendarEventSyncResult.Deleted, syncResult);
        }

        [TestMethod]
        public void Test_EventHasPendingSyncLog()
        {
            var startTime = DateTime.Now;
            var endTime = DateTime.Now.AddHours(2);

            var syncLogs = new Collection<SyncLog> { new SyncLog { CalendarEnd = endTime, CalendarStart = startTime, SyncDate = null } };

            var calendarEvent = new CalendarEvent { IsDeleted = false, SyncLogs = syncLogs };
            var calendarEventItem = new CalendarEventItem { Start = startTime, End = endTime };

            var syncResult = CalendarEventSynchronizer.SynchronizeCalendarEvent(calendarEvent, calendarEventItem);

            Assert.AreEqual(CalendarEventSynchronizer.CalendarEventSyncResult.PendingSyncLogs, syncResult);
        }

        [TestMethod]
        public void Test_EventMissingSyncLog()
        {
            var startTime = DateTime.Now;
            var endTime = DateTime.Now.AddHours(2);

            var calendarEvent = new CalendarEvent { IsDeleted = false, };
            var calendarEventItem = new CalendarEventItem { Start = startTime, End = endTime };

            var syncResult = CalendarEventSynchronizer.SynchronizeCalendarEvent(calendarEvent, calendarEventItem);

            Assert.AreEqual(CalendarEventSynchronizer.CalendarEventSyncResult.MissingSyncLogs, syncResult);
        }
    }
}
