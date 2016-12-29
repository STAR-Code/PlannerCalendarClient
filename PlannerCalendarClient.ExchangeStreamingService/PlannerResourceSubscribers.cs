using System;
using PlannerCalendarClient.Logging;
using PlannerCalendarClient.DataAccess;
using PlannerCalendarClient.ExchangeStreamingService.Affinity;

namespace PlannerCalendarClient.ExchangeStreamingService
{
    /// <summary>
    /// 
    /// </summary>
    internal class PlannerResourceSubscribers : SubscriberResourcesBase
    {
        private static readonly ILogger Logger = Logging.Logger.GetLogger();

        private DateTime? _lastRebuildSubscriptionGroupsTimestamp;
        private DateTime? _lastResourceUpdateTimestamp;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dbContextFactory"></param>
        /// <param name="exchangeStreamingConfig"></param>
        public PlannerResourceSubscribers(IClientDbEntitiesFactory dbContextFactory,IExchangeStreamingConfig exchangeStreamingConfig)
            : base(dbContextFactory, exchangeStreamingConfig)
        {
        }

        /// <summary>
        /// Get the planner resources with it's mail account. 
        /// </summary>
        /// <returns></returns>
        public SubscriptionGroupDictionary GetMailSubscriberLists(bool forceUpdate)
        {
            if (forceUpdate)
            {
                _lastRebuildSubscriptionGroupsTimestamp = null;
                _lastResourceUpdateTimestamp = null;
            }

            if (!_exchangeStreamingConfig.DeactivateSolvingOfGroupAffinity)
            {
                var rebuildSubscriptions = new BuildSubscriptionGroups(_dbContextFactory, _exchangeStreamingConfig);
                Logger.LogDebug(LoggingEvents.DebugEvent.General("Build/Rebuild the subscriber groups structure from the database. Last update timestamp: {0}".SafeFormat(_lastRebuildSubscriptionGroupsTimestamp)));
                rebuildSubscriptions.UpdateSubscriberResourcesGroupInformation(ref _lastRebuildSubscriptionGroupsTimestamp);
            }

            Logger.LogDebug(LoggingEvents.DebugEvent.General("Retrieve the subscriber groups structure from the database. Last opdate timestamp: {0}".SafeFormat(_lastResourceUpdateTimestamp)));

            using (var dbContext = _dbContextFactory.CreateClientDbEntities())
            {
                var subscriberMails = GetSubscriberResourcesWithGroupInfo(dbContext, ref _lastResourceUpdateTimestamp);

                if (subscriberMails != null)
                {
                    var groupedSubscribers = new SubscriptionGroupDictionary();

                    foreach (var s in subscriberMails)
                    {
                        var groupName = s.Subscription.Description;

                        if (groupedSubscribers.ContainsGroup(groupName))
                        {
                            groupedSubscribers.AddMailToGroup(groupName, s.MailAddress);
                        }
                        else
                        {
                            var userId = s.Subscription.ServiceUserCredential.UserId;
                            var password = s.Subscription.ServiceUserCredential.Password;

                            groupedSubscribers.CreateGroup(groupName, userId, password);
                            groupedSubscribers.AddMailToGroup(groupName, s.MailAddress);
                        }
                    }

                    return groupedSubscribers;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}