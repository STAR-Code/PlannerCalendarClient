using System;
using System.Collections.Generic;
using System.Linq;
using PlannerCalendarClient.Logging;
using PlannerCalendarClient.DataAccess;
using PlannerCalendarClient.ServiceDfdg;

namespace PlannerCalendarClient.PlannerCommunicatorService
{
    internal class CalendarEventManager
    {
        private static readonly ILogger Logger = Logging.Logger.GetLogger();

        private readonly ServiceConfiguration _configuration;
        private readonly IServiceRepository _serviceRepository;
        private readonly IECSClientExchangeDbEntities _entities;
        private readonly DateTime _lowerDateLimit;
        private readonly DateTime _upperDateLimit;

        /// <summary>
        /// .ctor
        /// </summary>
        public CalendarEventManager(IECSClientExchangeDbEntities entities, IServiceRepository serviceRepository, ServiceConfiguration configuration)
        {
            if (entities == null) throw new ArgumentNullException("entities");
            if (serviceRepository == null) throw new ArgumentNullException("serviceRepository");
            if (configuration == null) throw new ArgumentNullException("configuration");

            _entities = entities;
            _serviceRepository = serviceRepository;
            _configuration = configuration;
            _lowerDateLimit = DateTime.Today;
            _upperDateLimit = _lowerDateLimit.AddMonths(_configuration.CalendarEventsPeriod);
        }

        /// <summary>
        /// Sends pending calendar event updates to Planner
        /// New code that handle a single mailbox at the time.
        /// </summary>
        /// <param name="mailBox"></param>
        public void UpdateCalendarEvents(string mailBox)
        {
            if (mailBox == null) throw new ArgumentNullException("mailBox");

            // Get syncLogs that haven't already been synced
            var singleMailboxEvents = _entities.SyncLogs.Where(sl => sl.SyncDate == null && sl.CalendarEvent.MailAddress == mailBox).OrderBy(sl => sl.CreatedDate).ToList();

            Logger.LogDebug(LoggingEvents.DebugEvent.General(string.Format("Filter out {0} update for the mailbox \"{1}\".", singleMailboxEvents.Count(), mailBox)));

            var eventBucket = SyncLogBucket.GetEventBucket(singleMailboxEvents, Logger, _configuration.MaxCalendarEventUpdatesPerCall);

            SendCreateEvents(eventBucket.CreateEvents);
            SendUpdateEvents(eventBucket.UpdateEvents);
            SendDeleteEvents(eventBucket.DeleteEvents);

            Logger.LogDebug(LoggingEvents.DebugEvent.General(string.Format("Updates has been sent for the mailbox \"{0}\".", mailBox)));
        }

        private void SendCreateEvents(List<SyncLog> syncLogs)
        {
            if (!syncLogs.Any())
                return;

            var calendarEventItems = syncLogs.ToCalendarEventItems().ToList();
            var serviceCallReferenceItem = _serviceRepository.CreateEvents(calendarEventItems, _configuration.JobcenterNumber, _configuration.RequestUserIdentifier);
            var serviceCallReferenceLog = CalendarEventHelper.ToServiceCallReferenceItem(_entities, serviceCallReferenceItem);

            UpdateCalendarEventsSyncStatus(syncLogs, calendarEventItems, serviceCallReferenceLog);

            // Must handle those that got a SOAP exception
            ParseEventsForResend(syncLogs);

            // Must handle those that was sent as CREATE but was already in Planner
            ParseEventsAlreadyInPlannerAsNewUpdate(syncLogs);
        }

        private void ParseEventsAlreadyInPlannerAsNewUpdate(IEnumerable<SyncLog> syncLogs)
        {
            var alreadyInPlanner = syncLogs.Where(x => x.CreateWasUnsuccessfulItemAlreadyExists);
            foreach (var syncLogItem in alreadyInPlanner)
            {
                syncLogItem.CalendarEvent.SyncLogs.Add(syncLogItem.CopyToNew(Constants.SyncLogOperationUPDATE));
            }
        }

        private void ParseUpdateEventsNotInPlannerAsNewCreate(IEnumerable<SyncLog> syncLogs)
        {
            var notInPlanner = syncLogs.Where(x => x.UpdateWasUnsuccessfulItemNotInPlanner);
            foreach (var syncLogItem in notInPlanner)
            {
                syncLogItem.CalendarEvent.SyncLogs.Add(syncLogItem.CopyToNew(Constants.SyncLogOperationCREATE));
            }
        }

        private void ParseEventsForResend(IEnumerable<SyncLog> syncLogs)
        {
            var forResend = syncLogs.Where(x => x.QualifiesForResend);
            foreach (var syncLogItem in forResend)
            {
                syncLogItem.CalendarEvent.SyncLogs.Add(syncLogItem.CopyToNew(syncLogItem.Operation));
            }
        }

        private void SendUpdateEvents(List<SyncLog> syncLogs)
        {
            if (!syncLogs.Any())
                return;

            var calendarEventItems = syncLogs.ToCalendarEventItems().ToList();
            var serviceCallReferenceItem = _serviceRepository.UpdateEvents(calendarEventItems, _configuration.JobcenterNumber, _configuration.RequestUserIdentifier);
            var serviceCallReferenceLog = CalendarEventHelper.ToServiceCallReferenceItem(_entities, serviceCallReferenceItem);

            UpdateCalendarEventsSyncStatus(syncLogs, calendarEventItems, serviceCallReferenceLog);

            // Must handle those that got a SOAP exception
            ParseEventsForResend(syncLogs);

            // Must resend those with error 10 (not found in Planner) as create
            ParseUpdateEventsNotInPlannerAsNewCreate(syncLogs);
        }

        private void SendDeleteEvents(List<SyncLog> syncLogs)
        {
            if (!syncLogs.Any())
                return;

            var calendarEventItems = syncLogs.ToCalendarEventItems().ToList();
            var serviceCallReferenceItem = _serviceRepository.DeleteEvents(calendarEventItems, _configuration.JobcenterNumber, _configuration.RequestUserIdentifier);
            var serviceCallReferenceLog = CalendarEventHelper.ToServiceCallReferenceItem(_entities, serviceCallReferenceItem);

            UpdateCalendarEventsSyncStatus(syncLogs, calendarEventItems, serviceCallReferenceLog);

            // Must handle those that got a SOAP exception
            ParseEventsForResend(syncLogs);
        }

        private void UpdateCalendarEventsSyncStatus(IEnumerable<SyncLog> syncLogs, IEnumerable<CalendarEventItem> calendarEventItems, ServiceCallReferenceLog serviceCallReferenceLog)
        {
            var eventItems = calendarEventItems as IList<CalendarEventItem> ?? calendarEventItems.ToList();

            var syncLogCalendarEventsPair =
                from syncLog in syncLogs
                join calendarEventItem in eventItems
                        on new { syncLog.CalendarEvent.CalId, MailAddress = syncLog.CalendarEvent.MailAddress.ToLowerInvariant() } equals new { CalId = calendarEventItem.OriginId, MailAddress = calendarEventItem.OriginMailAddress.ToLowerInvariant() }
                select new { syncLog, calendarEventItem };

            foreach (var pair in syncLogCalendarEventsPair)
            {
                pair.syncLog.UpdateSyncLog(pair.calendarEventItem.SyncLogItem, serviceCallReferenceLog);

                if (!pair.syncLog.CalendarEvent.PlannerCalendarEventId.HasValue)
                {
                    pair.syncLog.CalendarEvent.PlannerCalendarEventId = pair.calendarEventItem.PlannerCalendarEventId;
                }

                pair.syncLog.CalendarEvent.PlannerResourceId = pair.calendarEventItem.PlannerResourceId;
            }
        }

        /// <summary>
        /// Synchronizes calendar events for active Planner resources
        /// </summary>
        public void SynchronizeCalendarEvents()
        {
            // Get planner resources
            var resources = _entities.PlannerResources.ToList().Where(x => x.IsQualifiedForSynchronization);

            // The service has no limit on the number of mailaddresses per request,
            // but for our own sake (to avoid timeout) we split into smaller chunks
            var mailAddressLists = resources.Select(r => r.MailAddress).ChunkBy(_configuration.MaxCalendarEventFetchesPerCall);

            foreach (var mailAddressList in mailAddressLists)
            {
                try
                {
                    List<CalendarEventItem> calendarEventItems;
                    if (!TryFetchEvents(mailAddressList, out calendarEventItems))
                        return;

                    Logger.LogDebug(LoggingEvents.DebugEvent.CalendarSynchronizerForMailAccounts(mailAddressList.ToArray(), calendarEventItems.Count));

                    SynchronizeCalendarEvents(calendarEventItems, mailAddressList);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, LoggingEvents.ErrorEvent.CalendarSynchronizerForMailAccountsException(mailAddressList.ToArray()));
                }
            }
        }

        private void SynchronizeCalendarEvents(IEnumerable<CalendarEventItem> calendarEventItems, List<string> mailAddressList)
        {
            foreach (var calendarEventItem in calendarEventItems)
            {
                var calEvent = _entities.CalendarEvents
                                .SingleOrDefault(e => e.MailAddress.Equals(calendarEventItem.OriginMailAddress, StringComparison.OrdinalIgnoreCase) && e.CalId == calendarEventItem.OriginId);

                if (calEvent == null)
                {
                    // CalendarEvent doesn't exist in PCC - the event must be deleted from Planner
                    var newCalendarEvent = CalendarEventSynchronizer.CreateNewCalendarEventForDeletion(calendarEventItem);
                    _entities.CalendarEvents.Add(newCalendarEvent);
                    Logger.LogDebug(LoggingEvents.DebugEvent.CreateCalendarEventForDeletion(newCalendarEvent.Id, newCalendarEvent.MailAddress, newCalendarEvent.CalId));
                }
                else
                    SynchronizeCalendarEvent(calEvent, calendarEventItem);
            }


            // If KClient CalendarEvent does not exist in Planner, create synclog with Constants.SyncLogOperationCREATE operation
            // Only compare calendarEvent with mailbox in mailAddressList! As calendarEventItems contains appointments only for mailbox in mailAddressList
            var Active = _entities.CalendarEvents
                        .Where(e => mailAddressList.Any(x => x.Equals(e.MailAddress, StringComparison.OrdinalIgnoreCase)))
                        .Where(e => !e.IsDeleted)
                        .Where(e => e.SyncLogs.Any(s => s.CalendarStart >= _lowerDateLimit && s.CalendarEnd <= _upperDateLimit))
                        .Select(e => new
                        {
                            calenderItem = e,
                            latestSynclog = e.SyncLogs.OrderByDescending(s => s.CreatedDate).FirstOrDefault()   //latestSynclog could be null
                        });

            foreach(var ac in Active)
            {
                if ( ac != null && ac.calenderItem != null && ac.latestSynclog != null && 
                    ac.latestSynclog.CalendarStart >= _lowerDateLimit && ac.latestSynclog.CalendarEnd <= _upperDateLimit &&
                    !calendarEventItems.Any(i => i.OriginId == ac.calenderItem.CalId && i.OriginMailAddress.Equals(ac.calenderItem.MailAddress, StringComparison.OrdinalIgnoreCase)))
                {
                    var newSync = ac.latestSynclog.CopySyncLog();
                    newSync.Operation = Constants.SyncLogOperationCREATE;
                    ac.calenderItem.SyncLogs.Add(newSync);

                    Logger.LogDebug(LoggingEvents.DebugEvent.CreateSyncLogForCreation(ac.calenderItem.Id, ac.calenderItem.MailAddress, ac.calenderItem.CalId));
                }
            }
        }

        private void SynchronizeCalendarEvent(CalendarEvent calendarEvent, CalendarEventItem calendarEventItem)
        {
            var syncResult = CalendarEventSynchronizer.SynchronizeCalendarEvent(calendarEvent, calendarEventItem);

            switch (syncResult)
            {
                case CalendarEventSynchronizer.CalendarEventSyncResult.PendingSyncLogs:
                    {
                        Logger.LogDebug(LoggingEvents.DebugEvent.PendingSyncLog(calendarEvent.Id, calendarEvent.MailAddress, calendarEvent.CalId));
                        break;
                    }
                case CalendarEventSynchronizer.CalendarEventSyncResult.UpToDate:
                    {
                        Logger.LogDebug(LoggingEvents.DebugEvent.EventUpToDate(calendarEvent.Id, calendarEvent.MailAddress, calendarEvent.CalId));
                        break;
                    }
                case CalendarEventSynchronizer.CalendarEventSyncResult.Deleted:
                    {
                        Logger.LogDebug(LoggingEvents.DebugEvent.CreateSyncLogForDeletion(calendarEvent.Id, calendarEvent.MailAddress, calendarEvent.CalId));
                        break;
                    }
                case CalendarEventSynchronizer.CalendarEventSyncResult.MissingSyncLogs:
                    {
                        Logger.LogError(LoggingEvents.ErrorEvent.FailedToSynchronizeCalendarEventMissingSyncLog(calendarEvent.Id, calendarEvent.MailAddress, calendarEvent.CalId));
                        break;
                    }
            }
        }

        private bool TryFetchEvents(IList<string> mailAddresses, out List<CalendarEventItem> calendarEventItems)
        {
            if (!mailAddresses.Any())
            {
                calendarEventItems = null;
                return false;
            }

            ServiceCallReferenceItem serviceCallReferenceItem;
            var items = _serviceRepository.GetEvents( _configuration.JobcenterNumber, 
                                                      _configuration.RequestUserIdentifier, 
                                                      mailAddresses, 
                                                      _lowerDateLimit, 
                                                      _upperDateLimit, 
                                                      out serviceCallReferenceItem);
            calendarEventItems = new List<CalendarEventItem>(items);

            // Possible duplicate, because in Planner duplicate {Mail, CalId}
            calendarEventItems = (from c in calendarEventItems
                                  group c by new { c.OriginId, c.OriginMailAddress } into grp
                                  select grp.FirstOrDefault()).ToList();

            return true;
        }
    }
}