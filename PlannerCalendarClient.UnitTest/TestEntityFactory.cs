using PlannerCalendarClient.DataAccess;

namespace PlannerCalendarClient.UnitTest
{
    class TestEntityFactory : IClientDbEntitiesFactory
    {
        private IECSClientExchangeDbEntities testContext;

        public TestEntityFactory(IECSClientExchangeDbEntities testContext)
        {
            this.testContext = testContext;
        }

        public IECSClientExchangeDbEntities CreateClientDbEntities()
        {
            return testContext;
        }
    }
}
