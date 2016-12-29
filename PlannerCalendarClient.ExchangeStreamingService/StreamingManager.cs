using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PlannerCalendarClient.DataAccess;
using PlannerCalendarClient.ExchangeServiceCreator;
using PlannerCalendarClient.ExchangeStreamingService.Affinity;
using PlannerCalendarClient.Logging;
using PlannerCalendarClient.Utility;

namespace PlannerCalendarClient.ExchangeStreamingService
{
    /// <summary>
    /// 
    /// </summary>
    public class StreamingManager : IDisposable
    {
        private static readonly ILogger Logger = Logging.Logger.GetLogger();

        private readonly IExchangeStreamingConfig _exchangeStreamingConfig;
        private readonly IClientDbEntitiesFactory _dbContextFactory;

        private readonly List<StreamingSubscriber> _currentStreamingSubscriptionGroups = new List<StreamingSubscriber>();
        private readonly PlannerResourceSubscribers _resourceSubscribers;
        private readonly IntervalCallbackTimer _updateMailAccountsTimer;
        private bool _lastUpdateUnsuccessful = true;

        private readonly object _lock = new object();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dbContextFactory"></param>
        /// <param name="exchangeStreamingConfig"></param>
        public StreamingManager(
            IClientDbEntitiesFactory dbContextFactory,
            IExchangeStreamingConfig exchangeStreamingConfig)
        {
            _dbContextFactory = dbContextFactory;

            _exchangeStreamingConfig = exchangeStreamingConfig;

            Logger.LogDebug(LoggingEvents.DebugEvent.CreateStreamingManager());

            _resourceSubscribers = new PlannerResourceSubscribers(_dbContextFactory, exchangeStreamingConfig);

            _updateMailAccountsTimer = new IntervalCallbackTimer(
                this.TimerCallUpdateMailAccounts,
                _exchangeStreamingConfig.SubscriberMailUpdateTimeInterval,
                "Exchange Streaming Service",
                true);
        }

        /// <summary>
        /// Start all streaming subscriptions
        /// </summary>
        public void Start()
        {
            try
            {
                lock (_lock)
                {
                    if (!_updateMailAccountsTimer.IsRunning())
                    {
                        _updateMailAccountsTimer.StartNow();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, LoggingEvents.ErrorEvent.StreamingManagerStartErrorUpdateMailAccounts);
            }
        }

        /// <summary>
        /// Stop all streaming subscriptions
        /// </summary>
        public void Stop()
        {
            try
            {
                // Release any outstanding retry waits, if any...
                ExchangeServerUtils.ForceCancelWait();

                lock (_lock)
                {
                    _updateMailAccountsTimer.Stop();

                    // Make sure to get a full independent list, because the current subscribers list will be modified in this call.
                    var currentGroups = _currentStreamingSubscriptionGroups.ConvertAll(i => i.Name).ToArray();
                    RemoveSubscriptionGroups(currentGroups);

                    Debug.Assert(_currentStreamingSubscriptionGroups.Count == 0,
                        "Program error. The streaming subscription collection is not empty as it should be when closing down.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, LoggingEvents.ErrorEvent.StreamingManagerStopError);
            }
        }

        /// <summary>
        /// If one or more subscription are running then true is returned.
        /// </summary>
        /// <returns>True if one or more subscription is running</returns>
        public bool IsRunning()
        {
            lock (_lock)
            {
                foreach (var subscriber in _currentStreamingSubscriptionGroups)
                {
                    try
                    {
                        if (subscriber.IsRunning())
                        {
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, LoggingEvents.ErrorEvent.StreamingManagerIsRunError(subscriber.Name));
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// The callback method that activate the check of change in the mails subscriptions.
        /// </summary>
        public void ForceUpdateMailAccounts()
        {
            try
            {
                if (_updateMailAccountsTimer.IsRunning() && _updateMailAccountsTimer.IsWaiting())
                {
                    lock (_lock)
                    {
                        UpdateMailAccounts();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, LoggingEvents.ErrorEvent.StreamingManagerTimerCallbackErrorUpdateMailAccounts);
            }
        }

        /// <summary>
        /// The callback method that activate the check of change in the mails subscriptions.
        /// </summary>
        private void TimerCallUpdateMailAccounts()
        {
            try
            {
                lock (_lock)
                {
                    UpdateMailAccounts();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, LoggingEvents.ErrorEvent.StreamingManagerTimerCallbackErrorUpdateMailAccounts);
            }
        }

        /// <summary>
        /// The callback method that activate the check of change in the mails subscriptions from the database.
        /// </summary>
        private void UpdateMailAccounts()
        {
            Logger.LogDebug(LoggingEvents.DebugEvent.StartUpdatingMailAccounts());

            // This method return null if not change has been made to the subscribed mail accounts and an empty list
            // if there are no mail account to subscribe to.
            var mails2SubscribeLists = _resourceSubscribers.GetMailSubscriberLists(_lastUpdateUnsuccessful);

            if (mails2SubscribeLists != null)
            {
                if (mails2SubscribeLists.Any())
                {
                    var currentGroups = _currentStreamingSubscriptionGroups.Select(i => i.Name).ToArray();

                    var groupsToRemove = currentGroups.Except(mails2SubscribeLists.GroupNames).ToArray();
                    var groupsToUpdate = mails2SubscribeLists.GroupNames.Intersect(currentGroups).ToArray();
                    var groupsToAdd = mails2SubscribeLists.GroupNames.Except(currentGroups).ToArray();

                    RemoveSubscriptionGroups(groupsToRemove);
                    UpdateSubscriptionGroups(groupsToUpdate, mails2SubscribeLists);
                    AddSubscriptionGroups(groupsToAdd, mails2SubscribeLists, _exchangeStreamingConfig);

                    // Should the service continue rinning when no mail account has been subscribe??
                    if (_currentStreamingSubscriptionGroups.Count == 0)
                    {
                        _lastUpdateUnsuccessful = true;
                        Logger.LogWarning(LoggingEvents.WarningEvent.NoSuccessfulNotificationSubscriptionCreated());
                        throw new ExchangeStreamConnectionException(LoggingEvents.WarningEvent.NoSuccessfulNotificationSubscriptionCreated());
                    }

                    _lastUpdateUnsuccessful = (_currentStreamingSubscriptionGroups.Count == groupsToUpdate.Count() + groupsToAdd.Count());
                    Logger.LogDebug(LoggingEvents.DebugEvent.General("Update result: {0}. Groups before update: {1}, After update {2} (Removed groups: {3}, Added groups: {4}, Updated groups {5}).".SafeFormat((_lastUpdateUnsuccessful?"Ok":"Failed"), currentGroups.Count(), _currentStreamingSubscriptionGroups.Count(), groupsToRemove.Count(), groupsToAdd.Count(), groupsToUpdate.Count())));
                }
                else
                {
                    Logger.LogWarning(LoggingEvents.WarningEvent.ErrorNoActiveSubscriptionMailAccountsInDb());
                }

                Logger.LogDebug(LoggingEvents.DebugEvent.FinishCheckingTheMailsSubscriptions());
            }
            else
            {
                Logger.LogDebug(LoggingEvents.DebugEvent.NoChangeInTheMailsSubscriptions());
            }

            // Push the needed updates to the current active subscriptions 
            // RefreshSubscriptionGroups();
        }

        /// <summary>
        /// Create new mail subscription groups
        /// </summary>
        /// <param name="groupsToAdd"></param>
        /// <param name="mails2SubscribeLists"></param>
        /// <param name="exchangeStreamingConfig"></param>
        private void AddSubscriptionGroups(
            string[] groupsToAdd, 
            SubscriptionGroupDictionary mails2SubscribeLists,
            IExchangeStreamingConfig exchangeStreamingConfig)
        {
            if (groupsToAdd != null && groupsToAdd.Any())
            {
                foreach (var groupName in groupsToAdd)
                {
                    var mailAccounts = mails2SubscribeLists.Group(groupName);

                    Logger.LogDebug(LoggingEvents.DebugEvent.AddSubscriptionGroup(groupName, mailAccounts.Mails.Count()));

                    try
                    {
                        var subscription = new StreamingSubscriber(_dbContextFactory, exchangeStreamingConfig, mailAccounts);
                        subscription.Start();
                        _currentStreamingSubscriptionGroups.Add(subscription);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, LoggingEvents.ErrorEvent.ErrorCreateGroupSubscription(groupName));
                    }
                }
            }
            else
            {
                Logger.LogDebug(LoggingEvents.DebugEvent.General("No new groups to add to the subscriptions."));
            }
        }

        /// <summary>
        /// Update mail subscription group's mail subscription by checking the subscribe mail accounts, removing account that is not in the group anymore and 
        /// adding new account to the subscription.
        /// </summary>
        /// <param name="groupsToUpdate"></param>
        /// <param name="mails2SubscribeLists"></param>
        private void UpdateSubscriptionGroups(string[] groupsToUpdate, SubscriptionGroupDictionary mails2SubscribeLists)
        {
            if (groupsToUpdate != null && groupsToUpdate.Any())
            {
                foreach (var groupName in groupsToUpdate)
                {
                    var mailAccounts = mails2SubscribeLists.Group(groupName);

                    Logger.LogDebug(LoggingEvents.DebugEvent.UpdateSubscriptionGroup(groupName, mailAccounts.Mails.Count()));

                    try
                    {
                        var subscriber = _currentStreamingSubscriptionGroups.Find(i => i.Name == groupName);
                        subscriber.UpdateSubscriptions(mailAccounts);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, LoggingEvents.ErrorEvent.ErrorUpdatingTheSubscriptionGroup(groupName));
                    }
                }
            }
            else
            {
                Logger.LogDebug(LoggingEvents.DebugEvent.General("No existing groups to update for the subscriptions."));
            }
        }

        /// <summary>
        /// Remove a subscription group
        /// </summary>
        /// <param name="groupsToRemove"></param>
        private void RemoveSubscriptionGroups(string[] groupsToRemove)
        {
            if (groupsToRemove != null && groupsToRemove.Any())
            {
                foreach (var groupName in groupsToRemove)
                {
                    try
                    {
                        var subscriber = _currentStreamingSubscriptionGroups.Find(i => i.Name == groupName);

                        Logger.LogDebug(LoggingEvents.DebugEvent.RemoveSubscriptionGroup(groupName, subscriber.MailSubscriptionCount()));

                        _currentStreamingSubscriptionGroups.Remove(subscriber);
                        subscriber.Stop();
                        subscriber.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, LoggingEvents.ErrorEvent.ErrorRemovingTheSubscriptionGroup(groupName));
                    }
                }
            }
            else
            {
                Logger.LogDebug(LoggingEvents.DebugEvent.General("No groups to remove from the subscriptions."));
            }
        }

        /// <summary>
        /// Make sure that all subscriptions are closed.
        /// </summary>
        public void Dispose()
        {
            try
            {
                _updateMailAccountsTimer.Dispose();
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, LoggingEvents.WarningEvent.DisposingError());
            }

            foreach (var subscriber in _currentStreamingSubscriptionGroups)
            {
                try
                {
                    subscriber.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, LoggingEvents.WarningEvent.DisposingError());
                }
            }

            _currentStreamingSubscriptionGroups.Clear();
        }
    }
}