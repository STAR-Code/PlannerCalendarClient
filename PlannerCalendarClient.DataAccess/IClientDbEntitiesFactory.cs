namespace PlannerCalendarClient.DataAccess
{
    public interface IClientDbEntitiesFactory
    {
        IECSClientExchangeDbEntities CreateClientDbEntities();
    }
}