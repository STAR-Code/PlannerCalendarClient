using PlannerCalendarClient.DataAccess;
using PlannerCalendarClient.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlannerCalendarClient.EventProcessorService
{
    internal static class EventProcessor
    {
        private static readonly ILogger Logger = Logging.Logger.GetLogger();

        internal static void ProcessNotifications(ServiceConfiguration configuration, IClientDbEntitiesFactory entityFactory, IAppointmentProviderFactory appointmentProviderFactory)
        {
            try
            {
                DateTime startDate;
                DateTime endDate;
                GetActiveCalendarPeriod(configuration, out startDate, out endDate);

                using (var dbContext = entityFactory.CreateClientDbEntities())
                {
                    // Load notifications from database
                    var notifications = dbContext.Notifications
                        .ToList();

                    if (notifications.Any())
                    {
                        Logger.LogDebug(LoggingEvents.DebugEvent.General("Got notified - retrieving appointment from Exchange..."));

                        // Connect to Exchange
                        var appointmentProvider = appointmentProviderFactory.GetProvider(configuration.SimultaniousCalls);

                        // Retrieve event data from Exchange in parallel
                        // Each notification may result in more then one appointments.
                        var notificationAndAppointments = notifications
                            .AsParallel().WithDegreeOfParallelism(configuration.SimultaniousCalls)
                            .Select(n =>
                            {
                                var notiAndAppoint = new NotificationAndAppointments
                                {
                                    Notification = n,
                                    Appointments = new List<IAppointment>()
                                };

                                try
                                {
                                    notiAndAppoint.Appointments = appointmentProvider
                                        .GetAppointmentsById(n.EwsId, startDate, endDate)
                                        .ToList();
                                }
                                catch (Exception ex)
                                {
                                    Logger.LogError(ex, LoggingEvents.ErrorEvent.ExceptionWhileRetrievingAppointment("NotificationId: {0}".SafeFormat(n.Id)));
                                    notiAndAppoint.ResponseText = ex.Message;
                                }
                                return notiAndAppoint;
                            })
                            .OrderBy(x => x.Notification.EwsTimestamp)
                            .ToList();

                        // Update database in serial
                        foreach (var notiAndAppoint in notificationAndAppointments)
                        {
                            try
                            {
                                foreach (var appointment in notiAndAppoint.Appointments)
                                {
                                    try
                                    {
                                        string operation;
                                        long? calendarEventId;

                                        if (appointment != null && DoesAppointmentQualifyForUpdate(dbContext, appointment, out operation, out calendarEventId))
                                        {
                                            Logger.LogDebug(LoggingEvents.DebugEvent.General("Appointment qualified for sync. Updating database..."));

                                            // CalendarEvent
                                            var calendarEvent = UpdateOrInsertCalendarEvent(dbContext, appointment, operation);

                                            // SyncLog
                                            var syncLog = new SyncLog
                                                {
                                                    CreatedDate = DateTime.Now,
                                                    CalendarEvent = calendarEvent,
                                                    CalendarStart = appointment.Start,
                                                    CalendarEnd = appointment.End,
                                                    Operation = operation,
                                                    NotificationLogId = notiAndAppoint.Notification.Id
                                                };
                                            dbContext.SyncLogs.Add(syncLog);
                                            dbContext.SaveChangesToDb();
                                        }
                                        else
                                        {
                                            Logger.LogDebug(LoggingEvents.DebugEvent.General("Appointment not qualified for sync."));
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.LogError(ex, LoggingEvents.ErrorEvent.ExceptionWhileQualifyingOrUpdatingDatabase(appointment.EmailAddress, appointment.ICalUid));
                                    }
                                }

                                MoveNotificationToLog(dbContext, notiAndAppoint.Notification, notiAndAppoint.ResponseText);
                                dbContext.SaveChangesToDb();
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, LoggingEvents.ErrorEvent.ExceptionWhileHandlingNotification(notiAndAppoint.Notification.Id, notiAndAppoint.Notification.EwsId));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, LoggingEvents.ErrorEvent.ExceptionThrown());
            }
        }

        /// <summary>
        /// Calculate the start date and end date for the calendar event to process. 
        /// Calendar event outside this interval should be ignored
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <param name="startDate">The calculated start date</param>
        /// <param name="endDate">The calculate end date</param>
        private static void GetActiveCalendarPeriod(ServiceConfiguration configuration, out DateTime startDate, out DateTime endDate)
        {
            startDate = DateTime.Today;
            endDate = startDate.AddMonths(configuration.CalendarEventsPeriodInMonths);
        }

        private static CalendarEvent UpdateOrInsertCalendarEvent(IECSClientExchangeDbEntities dbContext, IAppointment appointment, string operation)
        {
            var calendarEvent = dbContext.CalendarEvents
                .Where(ce => ce.CalId == appointment.ICalUid)
                .Where(ce => ce.MailAddress.Equals(appointment.EmailAddress, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (calendarEvent == null)
            {
                calendarEvent = new DataAccess.CalendarEvent
                {
                    CalId = appointment.ICalUid,
                    MailAddress = appointment.EmailAddress
                };
                dbContext.CalendarEvents.Add(calendarEvent);
            }

            calendarEvent.IsDeleted = operation == DataAccess.Constants.SyncLogOperationDELETE;
            return calendarEvent;
        }

        internal static void PerformFullAppointmentPull(ServiceConfiguration configuration, IClientDbEntitiesFactory entityFactory, IAppointmentProviderFactory appointmentProviderFactory)
        {
            int emailboxRetrievedTotal = 0;
            DateTime startTime = DateTime.Now;
            try
            {
                DateTime startDate;
                DateTime endDate;
                GetActiveCalendarPeriod(configuration, out startDate, out endDate);

                Logger.LogInfo(LoggingEvents.InfoEvent.FullPullStart(startDate, endDate));

                var appointmentProvider = appointmentProviderFactory.GetProvider(configuration.SimultaniousCalls);

                using (var dbContext = entityFactory.CreateClientDbEntities())
                {
                    // PlannerResource mailbox list
                    var resourceList = dbContext.PlannerResources.ToList().Where(x => x.IsQualifiedForSynchronization);

                    Logger.LogDebug(LoggingEvents.DebugEvent.General("Plan to retrieve {0} mailboxes from Exchange...", resourceList.Count()));

                    foreach (var r in resourceList.Select(a => new { mailbox = a.MailAddress, resourceId = a.PlannerResourceId }))
                    {
                        string mailbox = r.mailbox;
                        Guid? resourceId = r.resourceId;
                        List<IAppointment> appointments;
                        DateTime startTimeMailbox = DateTime.Now;
                        Logger.LogInfo(LoggingEvents.InfoEvent.FullPullStartMailbox(mailbox));

                        try
                        {
                            appointments = appointmentProvider.GetAppointmentsByMailbox(mailbox, startDate, endDate).ToList();
                            Logger.LogDebug(LoggingEvents.DebugEvent.General("Retrieved calendar items {3} from the mailbox '{0}' in the period {1} - {2} from Exchange and persist til db.", mailbox, startDate, endDate, appointments.Count));
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, LoggingEvents.ErrorEvent.ExceptionWhileRetrievingMailboxAppointment(mailbox));
                            Logger.LogInfo(LoggingEvents.InfoEvent.FullPullEndMailbox(mailbox, DateTime.Now.Subtract(startTimeMailbox)));
                            continue;
                        }

                        try
                        {
                            DeleteAppointmentsNotInView(dbContext, startDate, endDate, mailbox, appointments);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, LoggingEvents.ErrorEvent.ExceptionWhileDeletingMailboxAppointment(mailbox));
                            Logger.LogInfo(LoggingEvents.InfoEvent.FullPullEndMailbox(mailbox, DateTime.Now.Subtract(startTimeMailbox)));
                            continue;
                        }

                        try
                        {
                            appointments.ForEach(a => PersistExchangeAppointment(dbContext, a, mailbox, resourceId));
                            emailboxRetrievedTotal++;
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, LoggingEvents.ErrorEvent.ExceptionWhilePersistMailboxAppointment(mailbox));
                            Logger.LogInfo(LoggingEvents.InfoEvent.FullPullEndMailbox(mailbox, DateTime.Now.Subtract(startTimeMailbox)));
                            continue;
                        }

                        try
                        {
                            var resourceItem = resourceList.FirstOrDefault(x => x.PlannerResourceId.Equals(resourceId.Value));
                            if (resourceItem != null)
                            {
                                resourceItem.LastFullSync = DateTime.Now;
                                dbContext.SaveChangesToDb();
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, LoggingEvents.ErrorEvent.ErrorUpdateLastFullSyncForResource(mailbox, resourceId));
                        }

                        Logger.LogInfo(LoggingEvents.InfoEvent.FullPullEndMailbox(mailbox, DateTime.Now.Subtract(startTimeMailbox)));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, LoggingEvents.ErrorEvent.ExceptionThrown("PerformFullAppointmentPull"));
            }
            finally
            {
                Logger.LogDebug(LoggingEvents.DebugEvent.General("Retrieved {0} mailboxes from Exchange.".SafeFormat(emailboxRetrievedTotal)));
                Logger.LogInfo(LoggingEvents.InfoEvent.FullPullEnd(DateTime.Now.Subtract(startTime), emailboxRetrievedTotal));
            }
        }

        /// <summary>
        /// Mark CalendarEvents in database for deletion if they are not in the appointments collection
        /// </summary>
        /// <param name="dbContext">The data context to manipulate</param>
        /// <param name="startDate">Compare window start</param>
        /// <param name="endDate">Compare window end</param>
        /// <param name="mailbox">Mailbox for manipulation</param>
        /// <param name="appointments">Appointment collection from external provider</param>
        internal static void DeleteAppointmentsNotInView(IECSClientExchangeDbEntities dbContext, DateTime startDate, DateTime endDate, string mailbox, IEnumerable<IAppointment> appointments)
        {
            // Retrieve all mailbox CalendarEvent that are in the time window, along with the latest CalendarStart and CalendarEnd from SyncLog
            var activeAppointmentsInDb = dbContext.CalendarEvents
                .Where(ce => ce.MailAddress.Equals(mailbox, StringComparison.OrdinalIgnoreCase))
                .Where(ce => !ce.IsDeleted)
                .Where(ce => ce.SyncLogs.Any())
                .Select(ce => new
                {
                    CalendarEvent = ce,
                    Start = ce.SyncLogs.OrderByDescending(sl => sl.CreatedDate).FirstOrDefault().CalendarStart,
                    End = ce.SyncLogs.OrderByDescending(sl => sl.CreatedDate).FirstOrDefault().CalendarEnd
                })
                .Where(c => (startDate > c.Start && startDate < c.End)
                    || (endDate > c.Start && endDate < c.End)
                    || (startDate < c.Start && endDate > c.End))
                .ToList();

            // Narrow the collection down to appointments that are not in the appointment collection
            var mustBeMarkedIsDeleted = activeAppointmentsInDb
                .Where(dbApp => !appointments.Any(app => app.ICalUid == dbApp.CalendarEvent.CalId));

            // Mark these for deletion in the database
            foreach (var toBeDeleted in mustBeMarkedIsDeleted)
            {
                toBeDeleted.CalendarEvent.IsDeleted = true;
                toBeDeleted.CalendarEvent.SyncLogs.Add(new SyncLog
                {
                    Operation = Constants.SyncLogOperationDELETE,
                    CalendarStart = toBeDeleted.Start,
                    CalendarEnd = toBeDeleted.End,
                    CreatedDate = DateTime.Now
                });
                dbContext.SaveChangesToDb();
            }
        }

        internal static bool DoesAppointmentQualifyForUpdate(IECSClientExchangeDbEntities dbContext, IAppointment appointment, out string operation, out long? calendarEventId)
        {
            if (appointment == null)
                throw new ArgumentNullException("appointment");

            operation = null;
            bool doesQualify = false;

            // Get latest sync for this appointment
            var latestSyncLog = dbContext.SyncLogs
                .Where(sl => sl.CalendarEvent.CalId == appointment.ICalUid)
                .Where(sl => sl.CalendarEvent.MailAddress.Equals(appointment.EmailAddress, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(sl => sl.CreatedDate)
                .FirstOrDefault();

            calendarEventId = latestSyncLog != null ? latestSyncLog.CalendarEventId : (long?)null;

            // Only qualify new appointments that are after today - NOT today.
            // If the appointment is already known, change or delete operations will be qualified.
            if (appointment.Start.Date > DateTime.Today || latestSyncLog != null)
            {
                if (appointment.IsFree || appointment.IsCancelled || appointment.IsDeleted)
                    doesQualify = latestSyncLog != null && latestSyncLog.Operation != Constants.SyncLogOperationDELETE;
                else
                    doesQualify = latestSyncLog == null
                        || appointment.Start != latestSyncLog.CalendarStart
                        || appointment.End != latestSyncLog.CalendarEnd
                        || latestSyncLog.Operation == Constants.SyncLogOperationDELETE;

                if (doesQualify)
                {
                    // Determain what to do
                    if (appointment.IsCancelled || appointment.IsDeleted || appointment.IsFree)
                    {
                        operation = Constants.SyncLogOperationDELETE;
                    }
                    else if (latestSyncLog == null || latestSyncLog.Operation == Constants.SyncLogOperationDELETE)
                    {
                        if (appointment.Start == appointment.End)//Start=End => no busy time - hence do not qualify
                        {
                            doesQualify = false;
                        }
                        else
                        {
                            operation = Constants.SyncLogOperationCREATE;
                        }
                    }
                    else if (appointment.Start == appointment.End)//Start=End => Delete item (no more busy time)
                    {
                        operation = Constants.SyncLogOperationDELETE;
                    }
                    else if (appointment.Start.Date <= DateTime.Today.Date)
                    {
                        // Moved appointment to today - hence not longer relevant unless End is tomorrow or later
                        if (appointment.End.Date > DateTime.Today.Date)
                        {
                            operation = Constants.SyncLogOperationUPDATE;
                        }
                        else
                        {
                            operation = Constants.SyncLogOperationDELETE;
                        }
                    }
                    else
                    {
                        operation = Constants.SyncLogOperationUPDATE;
                    }
                }
            }
            return doesQualify;
        }

        private static void MoveNotificationToLog(IECSClientExchangeDbEntities dbContext, Notification notification, string responseText)
        {

            dbContext.NotificationLogs.Add(new NotificationLog
            {
                Id = notification.Id,
                EwsId = notification.EwsId,
                EwsTimestamp = notification.EwsTimestamp,
                ReceiveTime = notification.ReceiveTime,
                ProcessedTime = DateTime.Now,
                ResponseText = responseText
            });

            try
            {
                dbContext.Notifications.Remove(notification);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, LoggingEvents.ErrorEvent.ExceptionWhileDeletingNotification(notification.Id, notification.EwsId));
            }
        }

        private static bool PersistExchangeAppointment(IECSClientExchangeDbEntities dbContext, IAppointment appointment, string mailbox, Guid? plannerResourceId)
        {
            string op;
            long? calendarEventId;

            if (DoesAppointmentQualifyForUpdate(dbContext, appointment, out op, out calendarEventId))
            {
                if (calendarEventId == null)
                {
                    var calendarEvent = dbContext.CalendarEvents.Add(new CalendarEvent { MailAddress = mailbox, CalId = appointment.ICalUid, PlannerResourceId = plannerResourceId });
                    AddSynclog(dbContext, appointment, op, calendarEvent);
                }
                else
                {
                    AddSynclog(dbContext, appointment, op, calendarEventId.Value);
                }

                dbContext.SaveChangesToDb();
                return true;
            }

            return false;
        }

        private static void AddSynclog(IECSClientExchangeDbEntities dbContext, IAppointment appointment, string operation, CalendarEvent calendarEvent)
        {
            if (calendarEvent != null && operation != null)
            {
                dbContext.SyncLogs.Add(new SyncLog
                {
                    CalendarEvent = calendarEvent,
                    Operation = operation,
                    CalendarStart = appointment.Start,
                    CalendarEnd = appointment.End,
                    CreatedDate = DateTime.Now
                });
            }
        }

        private static void AddSynclog(IECSClientExchangeDbEntities dbContext, IAppointment appointment, string operation, long calendarEventId)
        {
            if (operation != null)
            {
                dbContext.SyncLogs.Add(new SyncLog
                {
                    CalendarEventId = calendarEventId,
                    Operation = operation,
                    CalendarStart = appointment.Start,
                    CalendarEnd = appointment.End,
                    CreatedDate = DateTime.Now
                });
            }
        }

        private class NotificationAndAppointments
        {
            public Notification Notification { get; set; }
            public IEnumerable<IAppointment> Appointments { get; set; }
            public string ResponseText { get; set; }
        }
    }
}
