using System;
using System.Collections.Generic;
using System.Linq;
using PlannerCalendarClient.DataAccess;
using PlannerCalendarClient.Logging;
using PlannerCalendarClient.ServiceDfdg;

namespace PlannerCalendarClient.PlannerCommunicatorService
{
    /// <summary>
    /// Sends calendar event updates to Planner
    /// </summary>
    internal static class CalendarUpdater
    {
        private static readonly ILogger Logger = Logging.Logger.GetLogger();

        internal static void ServiceProcessing(IClientDbEntitiesFactory dbContextFactory, ServiceConfiguration configuration)
        {
            int affectedMailBoxes = 0;
            DateTime startTime = DateTime.Now;
            try
            {
                List<string> mailBoxes;
                using (var entities = dbContextFactory.CreateClientDbEntities())
                {
                    mailBoxes = entities.SyncLogs.Where(sl => sl.SyncDate == null).Select(sl => sl.CalendarEvent.MailAddress).Distinct().ToList();
                }

                Logger.LogDebug(LoggingEvents.DebugEvent.General(string.Format("Found {0} mailboxes with calendar updates.", mailBoxes.Count())));

                mailBoxes.AsParallel().WithDegreeOfParallelism(configuration.SimultaniousCalls).ForAll
                (
                    // Run each mailboxes appointments separately because of planner preformere bad with appointments from mixed mailboxes
                    mailbox => UpdateMailCalendarEvent(dbContextFactory, configuration, mailbox)
                );

                affectedMailBoxes = mailBoxes.Count;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, LoggingEvents.ErrorEvent.CalendarUpdaterException());
            }

            Logger.LogInfo(LoggingEvents.InfoEvent.UpdateCalendarEventsCompleted(affectedMailBoxes, DateTime.Now.Subtract(startTime)));
        }

        /// <summary>
        /// Sends pending calendar event updates to Planner
        /// </summary>
        public static int UpdateMailCalendarEvent(IClientDbEntitiesFactory dbContextFactory, ServiceConfiguration configuration, string mailBox)
        {
            int affected = 0;

            try
            {
                using (var entities = dbContextFactory.CreateClientDbEntities())
                {
                    var calendarEvents = new CalendarEventManager(entities, new ServiceRepository(), configuration);
                    calendarEvents.UpdateCalendarEvents(mailBox);

                    affected = entities.SaveChangesToDb();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, LoggingEvents.ErrorEvent.CalendarUpdaterForMailBoxException(mailBox));
            }

            return affected;
        }
    }
}