using System;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using PlannerCalendarClient.DataAccess;
using PlannerCalendarClient.Logging;
using PlannerCalendarClient.ServiceDfdg;

namespace PlannerCalendarClient.PlannerCommunicatorService
{
    /// <summary>
    /// Synchronizes Planner calendars with PCC calendars
    /// </summary>
    internal static class CalendarSynchronizer
    {
        private static readonly ILogger Logger = Logging.Logger.GetLogger();

        internal static void ServiceProcessing(IClientDbEntitiesFactory dbContextFactory, ServiceConfiguration configuration)
        {
            DateTime startTime = DateTime.Now;
            int affected = 0;
            try
            {
                using (var entities = dbContextFactory.CreateClientDbEntities())
                {
                    var calendarEvents = new CalendarEventManager(entities, new ServiceRepository(), configuration);
                    calendarEvents.SynchronizeCalendarEvents();

                    affected = entities.SaveChangesToDb();
                }
            }
            catch(Exception ex)
            {
                Logger.LogError(ex, LoggingEvents.ErrorEvent.CalendarSynchronizerException());
            }
            Logger.LogInfo(LoggingEvents.InfoEvent.SynchronizationOfCalendarEventsCompleted(affected, DateTime.Now.Subtract(startTime)));
        }
    }
}