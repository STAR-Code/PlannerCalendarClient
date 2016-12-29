using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using PlannerCalendarClient.DataAccess;

namespace PlannerCalendarClient.ExchangeStreamingService
{
    internal class SubscriberResourcesBase
    {
        /// <summary>
        /// The exchange configuration object
        /// </summary>
        protected readonly IExchangeStreamingConfig _exchangeStreamingConfig;
        /// <summary>
        /// Database context factory
        /// </summary>
        protected readonly IClientDbEntitiesFactory _dbContextFactory;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dbContextFactory"></param>
        /// <param name="exchangeStreamingConfig"></param>
        protected SubscriberResourcesBase(IClientDbEntitiesFactory dbContextFactory, IExchangeStreamingConfig exchangeStreamingConfig)
        {
            _dbContextFactory = dbContextFactory;
            _exchangeStreamingConfig = exchangeStreamingConfig;
        }

        /// <summary>
        /// Get the resources mail accounts from planner in the scope of dbContext transaction.
        /// If null is returned then no change has been made since last call.
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="lastResourceUpdateTimestamp"></param>
        /// <returns></returns>
        protected IQueryable<PlannerResource> GetSubscriberResourcesWithoutGroupInfo(IECSClientExchangeDbEntities dbContext, ref DateTime? lastResourceUpdateTimestamp)
        {
            var plannerResources = from p in dbContext.PlannerResources where (p.GroupAffinity == null || p.Subscription == null) select p;

            // We need to have the delete marked planner resource to check for the last changed date 
            if (IsPlannerResourceUpdateNeeded(plannerResources, ref lastResourceUpdateTimestamp))
            {
                return plannerResources.Where(p => p.DeletedDate == null);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Get the resources mail accounts from planner in the scope of dbContext transaction.
        /// If null is returned then no change has been made since last call.
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="lastResourceUpdateTimestamp"></param>
        /// <returns></returns>
        protected IQueryable<PlannerResource> GetSubscriberResourcesWithGroupInfo(IECSClientExchangeDbEntities dbContext, ref DateTime? lastResourceUpdateTimestamp)
        {
            var plannerResources = from p in dbContext.PlannerResources where (p.GroupAffinity != null || p.Subscription != null) select p;

            //if (!plannerResources.Any())
            //    throw new ExchangeSubscriptionGroupException(LoggingEvents.WarningEvent.ErrorNoActiveSubscriptionMailAccountsInDb);

            // We need to have the delete marked planner resource to check for the last changed date 
            if (IsPlannerResourceUpdateNeeded(plannerResources, ref lastResourceUpdateTimestamp))
            {
                return plannerResources.Where(p => p.DeletedDate == null);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="plannerResources"></param>
        /// <param name="lastResourceUpdateTimestamp"></param>
        /// <returns></returns>
        private bool IsPlannerResourceUpdateNeeded(IQueryable<PlannerResource> plannerResources, ref DateTime? lastResourceUpdateTimestamp)
        {
            bool reprocessingNeeded = false;

            if (plannerResources.Any())
            {
                DateTime? latestResourceUpdateTimestamp =
                    plannerResources.Max(r => r.DeletedDate ?? (r.UpdatedDate ?? r.CreatedDate));

                if (latestResourceUpdateTimestamp.HasValue && latestResourceUpdateTimestamp.Value < DateTime.Now)
                {
                    if (lastResourceUpdateTimestamp == null ||
                        latestResourceUpdateTimestamp > lastResourceUpdateTimestamp)
                    {
                        lastResourceUpdateTimestamp = latestResourceUpdateTimestamp;

                        reprocessingNeeded = true;
                    }
                }
            }
            else
            {
                reprocessingNeeded = true;
            }

            return reprocessingNeeded;
        }
    }
}