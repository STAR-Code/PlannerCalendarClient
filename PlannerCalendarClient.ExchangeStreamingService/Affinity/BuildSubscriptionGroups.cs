using System;
using System.Collections.Generic;
using System.Linq;
using PlannerCalendarClient.DataAccess;
using PlannerCalendarClient.Logging;

namespace PlannerCalendarClient.ExchangeStreamingService.Affinity
{
    internal class BuildSubscriptionGroups : SubscriberResourcesBase
    {
        private static readonly ILogger Logger = Logging.Logger.GetLogger();

        #region class SubscriptionTree

        /// <summary>
        /// This clas contain an in memory structure of the database contents for easy iteration.
        /// 
        /// NOTE this class do only search for object in the memory and are updating the database objects relations!
        ///  
        /// Tree structure are in 3 levels:
        ///         Subscriptions  
        ///              Subscriptions
        ///                 PlannerResource
        /// </summary>
        private class SubscriptionTree
        {
            #region internal class define the tree

            private class SubscriptionGroups : Dictionary<Subscription, List<PlannerResource>> { }

            private class ServiceUserSubscriptionGroups : Dictionary<ServiceUserCredential, SubscriptionGroups> { }

            #endregion internal class define the tree

            private readonly int _maxSubscriptionPerConnection = 3;
            private readonly int _maxSubscriptionsPerSubscriptionGroup = 200;

            /// <summary>
            /// The internal tree structure
            /// </summary>
            private readonly ServiceUserSubscriptionGroups _subscriptionTree = new ServiceUserSubscriptionGroups();

            internal SubscriptionTree()
            {
            }

            internal SubscriptionTree(int maxSubscriptionsPerSubscriptionGroup, int maxSubscriptionPerConnection)
            {
                _maxSubscriptionsPerSubscriptionGroup = maxSubscriptionsPerSubscriptionGroup;
                _maxSubscriptionPerConnection = maxSubscriptionPerConnection;
            }

                // Build the menory representation tree structure of the database contents.

            /// <summary>
            /// Build the SubscriptionTree from the ServiceUserCredentials and its Substriptions and its mail accounts.
            /// </summary>
            /// <param name="serviceUserCredentials">The collection of service user credentials object</param>
            internal void BuildSubscriptionTree(IQueryable<ServiceUserCredential> serviceUserCredentials)
            {
                Logger.LogDebug(LoggingEvents.DebugEvent.General("SubscriptionTree setting, MaxSubscriptionPerConnection: {0}, MaxSubscriptionsPerSubscriptionGroup: {1}".SafeFormat(_maxSubscriptionPerConnection, _maxSubscriptionsPerSubscriptionGroup)));

                foreach (var serviceUserCredential in serviceUserCredentials)
                {
                    SubscriptionGroups subscriptionDic;

                    if (!_subscriptionTree.ContainsKey(serviceUserCredential))
                    {
                        subscriptionDic = new SubscriptionGroups();
                        _subscriptionTree.Add(serviceUserCredential, subscriptionDic);
                    }
                    else
                    {
                        subscriptionDic = _subscriptionTree[serviceUserCredential];
                    }

                    if (serviceUserCredential.Subscriptions.Any())
                    {
                        foreach (var subscription in serviceUserCredential.Subscriptions)
                        {
                            subscriptionDic.Add(subscription, subscription.PlannerResources.ToList());
                        }
                    }
                }
            }

            /// <summary>
            /// Look for the existing of the given mail account.
            /// This is done by looping through all mail accounts at the lowest level in the tree.
            /// </summary>
            /// <param name="mailAccount"></param>
            /// <returns>Return true if the mail account exist in the tree and false otherwise</returns>
            internal bool LookupMail(string mailAccount)
            {
                return (from serviceUserSubscriptionGroup in _subscriptionTree 
                        from subscription in serviceUserSubscriptionGroup.Value 
                        from plannerResource in subscription.Value 
                        select plannerResource).Any(plannerResource => mailAccount == plannerResource.MailAddress);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="plannerResource"></param>
            /// <returns></returns>
            internal bool AddSubscriptionToSubscriptionGroup(PlannerResource plannerResource)
            {
                ServiceUserCredential serviceUserWithFreeCapacity = null;

                if (_subscriptionTree.Count > 0)
                {
                    foreach (KeyValuePair<ServiceUserCredential, SubscriptionGroups> serviceUserSubscriptionGroup in _subscriptionTree)
                    {
                        // Remember the first service account with free subscription group capacity (for later use if needed)
                        if (serviceUserWithFreeCapacity == null && serviceUserSubscriptionGroup.Value.Count < _maxSubscriptionPerConnection)
                        {
                            serviceUserWithFreeCapacity = serviceUserSubscriptionGroup.Key;
                        }

                        KeyValuePair<Subscription, List<PlannerResource>>? foundSubscription = null;
                        foreach (KeyValuePair<Subscription, List<PlannerResource>> subscription in serviceUserSubscriptionGroup.Value)
                        {
                            // Is the affinity group the rigth one and are there space for a new subscription group.
                            if (subscription.Key.GroupAffinity == plannerResource.GroupAffinity &&
                                subscription.Value.Count < _maxSubscriptionsPerSubscriptionGroup)
                            {
                                foundSubscription = subscription;
                                break; // It is not possible to update the connection when doing the iteration.
                            }
                        }

                        if (foundSubscription != null)
                        {
                            // Update the memory structure
                            foundSubscription.Value.Value.Add(plannerResource);
                            // Update the database relations
                            plannerResource.Subscription = foundSubscription.Value.Key;
                            plannerResource.Subscription.UpdatedDate = DateTime.Now;
                            return true;
                        }
                    }
                }
                else
                {
                    throw new ExchangeSubscriptionGroupException(LoggingEvents.ErrorEvent.ErrorNoServiceUserAccountForSubscription);
                }

                // No existing subscription could be use! Create a new subscription
                if (serviceUserWithFreeCapacity != null)
                {
                    var subscriptionGroups = _subscriptionTree[serviceUserWithFreeCapacity];

                    var subscriptionGroup = new Subscription()
                    {
                        GroupAffinity = plannerResource.GroupAffinity,
                        Description = SafeStringFormat.SafeFormat("Group no. {0}", subscriptionGroups.Count + 1),
                        // Update the database relations
                        ServiceUserCredential = serviceUserWithFreeCapacity,
                        CreatedDate = DateTime.Now,
                    };

                    // Update the memory structure
                    subscriptionGroups.Add(subscriptionGroup, new List<PlannerResource>());
                    // Update the database relations
                    plannerResource.Subscription = subscriptionGroup;

                    return true;
                }
                else
                {
                    // Maybe the code should write out the current structure to the log file.
                    int serviceAcountCount = _subscriptionTree.Count;
                    int subscriptionCount = _subscriptionTree.Sum(x => x.Value.Count);

                    throw new ExchangeSubscriptionGroupException(LoggingEvents.ErrorEvent.ErrorNoFreeServiceUserAccountTheNewSubscriptionGroup(plannerResource.GroupAffinity, serviceAcountCount, subscriptionCount));
                }
            }

            /// <summary>
            /// Get all the subscriptions as a list (From the internal tree structure)
            /// </summary>
            /// <returns></returns>
            internal IEnumerable<Subscription> GetSubscriptions()
            {
                var list = new List<Subscription>();

                foreach (KeyValuePair<ServiceUserCredential, SubscriptionGroups> serviceUserSubscriptionGroup in _subscriptionTree)
                {
                    list.AddRange(serviceUserSubscriptionGroup.Value.Keys);
                }

                return list;
                
            }
        }

        #endregion class SubscriptionTree

        public BuildSubscriptionGroups(IClientDbEntitiesFactory dbContextFactory, IExchangeStreamingConfig exchangeStreamingConfig)
            : base(dbContextFactory, exchangeStreamingConfig)
        {
            Logger.LogDebug(LoggingEvents.DebugEvent.General("Create the BuildSubscriptionGroups object"));
        }

        /// <summary>
        /// Find the mail accounts without a group affinity and assign the affinity to the database table PlannerResource field GroupAffinity
        /// </summary>
        /// <param name="lastResourceUpdateTimestamp"></param>
        public void UpdateSubscriberResourcesGroupInformation(ref DateTime? lastResourceUpdateTimestamp)
        {
            Logger.LogDebug(LoggingEvents.DebugEvent.General("Rebuild/Update the subscriber group structure for the database"));

            List<string> mailAccountWithoutGroupInfo;

            // Find the mail addresses that need get a GroupAffinity
            try
            {
                mailAccountWithoutGroupInfo = MailAccountWithoutGroupInfo(ref lastResourceUpdateTimestamp);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, LoggingEvents.ErrorEvent.ErrorRetrievingTheMailSubscriptionFromDb());

                return;
            }

            if (mailAccountWithoutGroupInfo == null)
                return;
            
            // Collect the mail account's server group affinity
            var groupAffinities = CollectionMailAccountsGroupAffinity(mailAccountWithoutGroupInfo);

            // Update the database with the new group affinity for the mails
            List<string> mailAccountWithSuccessfulUpdatedGroupInfo;
            try
            {
                mailAccountWithSuccessfulUpdatedGroupInfo = UpdateTheMailAccountsWithGroupInfo(groupAffinities);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, LoggingEvents.ErrorEvent.ErrorSavingTheMailSubscriptionToDb());

                return;
            }

            if (mailAccountWithSuccessfulUpdatedGroupInfo.Count == 0)
                return;

            try
            {
                ReassignSubscriberGroups(mailAccountWithSuccessfulUpdatedGroupInfo);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, LoggingEvents.ErrorEvent.ErrorSavingTheMailSubscriptionToDb());
                return;
            }

            Logger.LogInfo(LoggingEvents.InfoEvent.SuccessfulUpdatedTheSubscriptionGroupToDb());
        }

        private List<string> UpdateTheMailAccountsWithGroupInfo(List<GroupAffinitySolver.MailGroupAffinity> groupAffinities)
        {
            var updateTimestamp = DateTime.Now;

            var mailAccountWithSuccessfulUpdatedGroupInfo = new List<string>();

            using (var dbContext = _dbContextFactory.CreateClientDbEntities())
            {
                var plannerResources = dbContext.PlannerResources;

                foreach (var affinity in groupAffinities)
                {
                    try
                    {
                        var rec = plannerResources.SingleOrDefault(i => i.MailAddress == affinity.MailAddress);

                        rec.ErrorDate = affinity.UpdateDate;

                        bool updated = false;
                        if (affinity.ErrorEvent == null)
                        {

                            if (rec.GroupAffinity == null || rec.GroupAffinity != affinity.GroupingKey)
                            {
                                rec.GroupAffinity = affinity.GroupingKey; // Update the subscription group
                                updated = true;
                            }

                            if (rec.Subscription != null && rec.Subscription.GroupAffinity != affinity.GroupingKey)
                            {
                                rec.Subscription = null; // clear the old wrong subscription group
                                updated = true;
                            }

                            if (rec.ErrorCode != null)
                            {
                                rec.ErrorCode = null;     // clear the old error message
                                rec.ErrorDescription = null;
                                updated = true;
                            }

                            mailAccountWithSuccessfulUpdatedGroupInfo.Add(affinity.MailAddress);
                        }
                        else
                        {
                            rec.ErrorCode = affinity.ErrorEvent.EventId.ToString();
                            rec.ErrorDescription = affinity.ErrorMessage;
                            // Should the GroupAffinity be removed or not?
                            if (rec.GroupAffinity != null)
                            {
                                rec.GroupAffinity = null; // clear the old wrong subscription group
                                rec.Subscription = null;
                                updated = true;
                            }
                        }

                        if (updated)
                        {
                            rec.UpdatedDate = updateTimestamp;  // Set the update timestamp
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, LoggingEvents.ErrorEvent.TheMailAccountIsNotFoundForGroupAffinityAssigning(affinity.MailAddress));
                    }
                }

                dbContext.SaveChangesToDb();
            }

            return mailAccountWithSuccessfulUpdatedGroupInfo;
        }

        private List<string> MailAccountWithoutGroupInfo(ref DateTime? lastResourceUpdateTimestamp)
        {
            using (var dbContext = _dbContextFactory.CreateClientDbEntities())
            {
                var mailAccounts = GetSubscriberResourcesWithoutGroupInfo(dbContext, ref lastResourceUpdateTimestamp);
                if (mailAccounts != null)
                {
                    return (from m in mailAccounts select m.MailAddress).ToList();
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Return the exchange group affinity for the mail accounts. Only mail accounts with that has been 
        /// successful resolved by there group affinity are returned. Mail account that fails to resolve are
        /// logged.
        /// 
        /// The group affinity are resolve by calling exchange for each mail account.
        /// </summary>
        /// <param name="mailAccountWithoutGroupInfo"></param>
        /// <returns>A list of group affinities for the mail accounts</returns>
        private List<GroupAffinitySolver.MailGroupAffinity> CollectionMailAccountsGroupAffinity(List<string> mailAccountWithoutGroupInfo)
        {
            var exchangeUserSettings = new GroupAffinitySolver(_exchangeStreamingConfig);

            // Get each mail accounts affinity from the exchange server. In case of an error it is logged in the MailGroupAffinity object.
            var groupAffinities = mailAccountWithoutGroupInfo.AsParallel().WithDegreeOfParallelism(1).Select(exchangeUserSettings.GetMailAccountGroupingInformationSettings).ToList();

            return groupAffinities;
        }

        /// <summary>
        /// Rebuild the subscription groups by using the mail accounts group affinity.
        /// </summary>
        /// <param name="mailAccountsWithUpdatedGroupInfo">List of mail accounts with updated affinity group information</param>
        private void ReassignSubscriberGroups(List<string> mailAccountsWithUpdatedGroupInfo)
        {
            CreateDefaultServiceUserAccountIfNeeded();

            using (var dbContext = _dbContextFactory.CreateClientDbEntities())
            {
                var subscriptionTree = new SubscriptionTree(_exchangeStreamingConfig.MaxSubscriptionPerConnection, _exchangeStreamingConfig.MaxSubscriptionsPerSubscriptionGroup);

                subscriptionTree.BuildSubscriptionTree(dbContext.ServiceUserCredentials);

                // Assign the mail account that miss a subscription group to a group.
                foreach (var mailAccount in mailAccountsWithUpdatedGroupInfo)
                {
                    // It a new mail account
                    var plannerResource = dbContext.PlannerResources.Single(r => r.MailAddress == mailAccount);

                    if (!subscriptionTree.LookupMail(mailAccount))
                    {
                        if (!subscriptionTree.AddSubscriptionToSubscriptionGroup(plannerResource))
                        {
                            // TODO: Error the mail could not be assign to a subscription group!!
                        }
                    }
                    else
                    {
                        // The mail exist in the subscription tree, but maybe with a wrong exchange subscription group?
                    }
                }

                dbContext.SaveChangesToDb();

                // Remove all the empty subscription items.
                foreach (var subscription in subscriptionTree.GetSubscriptions())
                {
                    if (!subscription.PlannerResources.Any())
                    {
                        dbContext.Subscriptions.Remove(subscription);
                    }
                }

                dbContext.SaveChangesToDb();
            }
        }

        /// <summary>
        /// This method check to see if any service user exist in the database. If not then a default
        /// service user is created. This user has a blank user name and password and will fallback to 
        /// use the default credentials (the windows service user).
        /// Note: A service user can only support a limit number of current mail subscriptions (See the 
        /// Exchange documentation)
        /// </summary>
        private void CreateDefaultServiceUserAccountIfNeeded()
        {
            using (var dbContext = _dbContextFactory.CreateClientDbEntities())
            {
                if(!dbContext.ServiceUserCredentials.Any())
                {
                    dbContext.ServiceUserCredentials.Add(new ServiceUserCredential {UserId = "", Password = ""});
                    dbContext.SaveChangesToDb();
                    Logger.LogDebug(LoggingEvents.DebugEvent.General("Create a default service user in the database."));
                }
            }
        }
    }
}