
namespace PlannerCalendarClient.EventProcessorService
{
    internal interface IAppointmentProviderFactory
    {
        IAppointmentProvider GetProvider(int maxParallelism);
    }
}
