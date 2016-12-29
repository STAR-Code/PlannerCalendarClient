using PlannerCalendarClient.Logging;
using System;

namespace PlannerCalendarClient.EventProcessorService
{
    internal class ExchangeAppointmentProviderFactory : IAppointmentProviderFactory
    {
        private static readonly ILogger Logger = Logging.Logger.GetLogger();

        private readonly string[] _args;

        public ExchangeAppointmentProviderFactory(string[] args)
        {
            _args = args;
        }

        public IAppointmentProvider GetProvider(int maxParallelism)
        {
            string serviceUserEmailAccount = null;
            try
            {
                var exchConfig = ExchangeServiceCreator.Config.GetConnectionConfig(_args);
                serviceUserEmailAccount = exchConfig.ServerUserEmailAccount;
                var exchangeService = PlannerCalendarClient.ExchangeServiceCreator.CreateExchangeServiceConnection.ConnectToService(exchConfig);
                var exchangeGateway = new ExchangeGateway(exchangeService, serviceUserEmailAccount, exchConfig.UseImpersonation);
                var exchangeRecurrenceAppointmentSolver = new ExchangeRecurrenceAppointmentSolver(exchangeGateway);

                return new ExchangeAppointmentProvider(exchangeGateway, exchangeRecurrenceAppointmentSolver, maxParallelism);
            }
            catch(Exception ex)
            {
                Logger.LogError(ex, LoggingEvents.ErrorEvent.ExceptionWhileGettingProvider(serviceUserEmailAccount));
                throw ex;
            }
        }
    }
}
