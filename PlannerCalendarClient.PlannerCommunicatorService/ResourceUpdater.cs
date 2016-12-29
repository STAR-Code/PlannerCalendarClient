using System;
using PlannerCalendarClient.DataAccess;
using PlannerCalendarClient.Logging;
using PlannerCalendarClient.ServiceDfdg;

namespace PlannerCalendarClient.PlannerCommunicatorService
{
    internal static class ResourceUpdater
    {
        private static readonly ILogger Logger = Logging.Logger.GetLogger();

        internal static void ServiceProcessing(IClientDbEntitiesFactory dbContextFactory, ServiceConfiguration configuration)
        {
            try
            {
                DateTime startTime = DateTime.Now;
                // Update the list of resources
                using (var entities = dbContextFactory.CreateClientDbEntities() )
                {
                    Logger.LogInfo(LoggingEvents.InfoEvent.UpdateResourcesStarting());

                    var updateResources = new SubscriberResources(entities, new ServiceRepository(), configuration.JobcenterNumber, configuration.RequestUserIdentifier);
                    updateResources.UpdateSubscriberResources();

                    try
                    {
                        var affected = entities.SaveChangesToDb();
                        Logger.LogInfo(LoggingEvents.InfoEvent.UpdatedResourcesFinished(affected, DateTime.Now.Subtract(startTime)));
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, LoggingEvents.ErrorEvent.ResourceUpdaterSaveDbException());
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, LoggingEvents.ErrorEvent.ResourceUpdaterException());
            }
        }
    }
}