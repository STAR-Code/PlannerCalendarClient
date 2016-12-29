using PlannerCalendarClient.EventProcessorService;

namespace PlannerCalendarClient.UnitTest.EventProcessorService
{
    internal class TestAppointmentProviderFactory: IAppointmentProviderFactory
    {
        public IAppointmentProvider GetProvider(int maxParallelism)
        {
            return new TestAppointmentProvider();
        }
    }
}
