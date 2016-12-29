
using System.Text;
using PlannerCalendarClient.Logging;

namespace PlannerCalendarClient.DataAccess
{
    /// <summary>
    /// The purpose of this partial part of ECSClientExchangeDbEntities is to relate it to the IECSClientExchangeDbEntities interface
    /// </summary>
    public partial class ECSClientExchangeDbEntities : IECSClientExchangeDbEntities
    {
        private static readonly ILogger Logger = Logging.Logger.GetLogger();

        /// <summary>
        /// Return the database name.
        /// </summary>
        /// <returns>The database name</returns>
        public string DataSource()
        {
            return this.Database.Connection.DataSource + "\\" + this.Database.Connection.Database;
        }


        public int SaveChangesToDb()
        {
            try
            {
                return this.SaveChanges();
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException dbException)
            {
                var sb = new StringBuilder();

                // Collect the information about the reason to the error
                foreach (System.Data.Entity.Validation.DbEntityValidationResult errInfo in dbException.EntityValidationErrors)
                {
                    sb.AppendLine();
                    foreach (System.Data.Entity.Validation.DbValidationError valError in errInfo.ValidationErrors)
                    {
                        sb.AppendFormat("Property name: {0} : {1}", valError.PropertyName, valError.ErrorMessage);
                        sb.AppendLine();
                    }
                }

                Logger.LogError(LoggingEvents.ErrorEvent.DataSaveExceptionDetail(sb.ToString()));

                throw;
            }
        }
    }
}
