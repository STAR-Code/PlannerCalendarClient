using System;

namespace PlannerCalendarClient.DataAccess
{
    public interface IECSClientExchangeDbEntities : IDisposable
    {
        string DataSource();

        System.Data.Entity.DbSet<CalendarEvent> CalendarEvents { get; set; }
        System.Data.Entity.DbSet<NotificationLog> NotificationLogs { get; set; }
        System.Data.Entity.DbSet<Notification> Notifications { get; set; }
        System.Data.Entity.DbSet<ServiceCallReferenceLog> ServiceCallReferenceLogs { get; set; }
        System.Data.Entity.DbSet<SyncLog> SyncLogs { get; set; }
        System.Data.Entity.DbSet<PlannerResource> PlannerResources { get; set; }
        System.Data.Entity.DbSet<PlannerResourceWhitelist> PlannerResourceWhitelists { get; set; }
        System.Data.Entity.DbSet<PlannerResourceBlacklist> PlannerResourceBlacklists { get; set; }
        System.Data.Entity.DbSet<ServiceUserCredential> ServiceUserCredentials { get; set; }
        System.Data.Entity.DbSet<Subscription> Subscriptions { get; set; }

        int SaveChangesToDb();
    }
}
