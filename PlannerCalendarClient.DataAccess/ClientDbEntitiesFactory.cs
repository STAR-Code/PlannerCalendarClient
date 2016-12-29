using System;
using System.Linq;
using PlannerCalendarClient.Logging;
using System.Configuration;

namespace PlannerCalendarClient.DataAccess
{
    public class ClientDbEntitiesFactory : IClientDbEntitiesFactory
    {
        private static readonly ILogger Logger = Logging.Logger.GetLogger();

        /// <summary>
        /// Create the Db entity factory.
        /// 
        /// Beside that it validate that the connection can be made to the database.
        /// </summary>
        public ClientDbEntitiesFactory()
        {
            var conStr = ConfigurationManager.ConnectionStrings["ECSClientExchangeDbEntities"];
            if (conStr == null)
            {
                var errEvt = LoggingEvents.ErrorEvent.ConfigurationOfECSClientExchangeDbEntitiesFailed();
                Logger.LogError(errEvt);
                throw new ConfigurationErrorsException(errEvt.Message);
            }

            using (var dbContext = new ECSClientExchangeDbEntities())
            {
                // Write the database path to the log
                var path = dbContext.DataSource();
                
                // If local machine then replace the . with the machine name.
                if (path.StartsWith(".\\"))
                {
                    path = Environment.MachineName + path.Substring(1);
                }

                Logger.LogDebug(LoggingEvents.DebugEvent.DataSource(path));
                
                try
                {
                    // Test database connection before returning
                    // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                    dbContext.Notifications.Any();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, LoggingEvents.ErrorEvent.VerifyDatabaseConnectionFailed(path));
                    throw;
                }
            }
        }

        public virtual IECSClientExchangeDbEntities CreateClientDbEntities()
        {
            return new ECSClientExchangeDbEntities();
        }
    }
}