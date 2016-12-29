using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlannerCalendarClient.DataAccess;
using PlannerCalendarClient.Logging;
using PlannerCalendarClient.PlannerCommunicatorService;

namespace PlannerCalendarClient.UnitTest.PlannerCommunicatorService
{
    [TestClass]
    public class TestEventBucket
    {
        private ILogger _logger;

        [TestInitialize]
        public void SetupTest()
        {
            _logger = new TestLogger();
        }
        
        [TestMethod]
        public void Test_GetEventBucket_MaxSizeReached()
        {
            var events = GetTestEvents().ToList();
            events.InsertRange(5, GetTestEvents(5, "U"));
            events.InsertRange(10, GetTestEvents(5, "D"));
            
            var bucket = SyncLogBucket.GetEventBucket(events, _logger, 10);

            Assert.AreEqual(bucket.CreateEvents.Count(), 10);
            Assert.AreEqual(bucket.UpdateEvents.Count(), 5);
            Assert.AreEqual(bucket.DeleteEvents.Count(), 5);
        }

        [TestMethod]
        public void Test_GetEventBucket_MaxSizeNotReached()
        {
            var events = GetTestEvents().ToList();
            events.InsertRange(5, GetTestEvents(5, "U"));
            events.InsertRange(10, GetTestEvents(5, "D"));

            var bucket = SyncLogBucket.GetEventBucket(events, _logger, 100);

            Assert.AreEqual(bucket.CreateEvents.Count(), 20);
            Assert.AreEqual(bucket.UpdateEvents.Count(), 5);
            Assert.AreEqual(bucket.DeleteEvents.Count(), 5);
        }

        [TestMethod]
        public void Test_GetEventBucket_UpdateEventsAggregation()
        {
            var events = GetTestEvents().ToList();
            var updateEvents = GetTestEvents(5, "U").ToList();
            updateEvents.Take(4).ToList().ForEach(u => u.CalendarEventId = 666); // Set the same calendarID for 4 updates - only the last of the 4 should be put in the bucket
            
            events.InsertRange(5,updateEvents);
            var bucket = SyncLogBucket.GetEventBucket(events, _logger, 10);

            Assert.AreEqual(bucket.CreateEvents.Count(), 10);
            Assert.AreEqual(bucket.UpdateEvents.Count(), 2);
            Assert.AreEqual(bucket.DeleteEvents.Count(), 0);
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
                    CreatedDate = DateTime.Now + new TimeSpan(0,0,0,i),
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
}
