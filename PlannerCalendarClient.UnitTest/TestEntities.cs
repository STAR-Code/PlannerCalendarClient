using PlannerCalendarClient.DataAccess;
using System;
using System.Collections.Generic;
using System.Data.Entity;

namespace PlannerCalendarClient.UnitTest
{
    /// <summary>
    /// This stub is implemented with inspiration from <see cref="http://www.asp.net/web-api/overview/testing-and-debugging/mocking-entity-framework-when-unit-testing-aspnet-web-api-2#testcontext"/>
    /// </summary>
    public class TestEntities : IECSClientExchangeDbEntities
    {
        public DbSet<CalendarEvent> CalendarEvents { get; set; }
        public DbSet<NotificationLog> NotificationLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<PlannerResource> PlannerResources { get; set; }
        public DbSet<PlannerResourceWhitelist> PlannerResourceWhitelists { get; set; }
        public DbSet<PlannerResourceBlacklist> PlannerResourceBlacklists { get; set; }
        public DbSet<ServiceCallReferenceLog> ServiceCallReferenceLogs { get; set; }
        public DbSet<SyncLog> SyncLogs { get; set; }
        public DbSet<ServiceUserCredential> ServiceUserCredentials { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }

        public TestEntities()
        {
            CalendarEvents = new TestDbSet<CalendarEvent>();
            Notifications = new TestDbSet<Notification>();
            NotificationLogs = new TestDbSet<NotificationLog>();
            PlannerResources = new TestDbSet<PlannerResource>();
            PlannerResourceWhitelists = new TestDbSet<PlannerResourceWhitelist>();
            PlannerResourceBlacklists = new TestDbSet<PlannerResourceBlacklist>();
            ServiceCallReferenceLogs = new TestDbSet<ServiceCallReferenceLog>();
            SyncLogs = new TestDbSet<SyncLog>();
            ServiceUserCredentials = new TestDbSet<ServiceUserCredential>();
            Subscriptions = new TestDbSet<Subscription>();
        }

        public string DataSource()
        {
            return "UnitTestDb";
        }

        public int SaveChangesToDb()
        {
            return 0;
        }

        public void Dispose()
        {
        }

        private Dictionary<string, int> Identifiers = new Dictionary<string, int>();

        internal int GetIdentifyer(string name)
        {
            if (Identifiers.ContainsKey(name))
            {
                Identifiers[name]++;
                return Identifiers[name];
            }
            else
            {
                Identifiers[name] = 1;
                return 1;
            }
        }
    }

    public static class TestEntitiesExtensions
    {
        public static SyncLog AddSyncLog(this CalendarEvent calendarEvent, TestEntities entities, DateTime start, DateTime end, string operation)
        {
            return calendarEvent.AddSyncLog(entities, start, end, operation, DateTime.Now);
        }

        public static SyncLog AddSyncLog(this CalendarEvent calendarEvent, TestEntities entities, DateTime start, DateTime end, string operation, DateTime created)
        {
            int id = entities.GetIdentifyer("SyncLog");
            var synclog = new SyncLog
            {
                Id = id,
                CalendarStart = start,
                CalendarEnd = end,
                Operation = operation,
                CalendarEvent = calendarEvent,
                CalendarEventId = calendarEvent.Id,
                CreatedDate = created
            };

            entities.SyncLogs.Add(synclog);
            calendarEvent.SyncLogs.Add(synclog);

            return synclog;
        }
    }
}
