using System;
using System.ServiceProcess;
using PlannerCalendarClient.DataAccess;
using PlannerCalendarClient.ExchangeStreamingService;
using PlannerCalendarClient.Logging;
using PlannerCalendarClient.Utility;

namespace PlannerCalendarClient.ExchangeListenerService
{
    static class Program
    {
        private static readonly ILogger Logger = Logging.Logger.GetLogger();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static int Main(string[] args)
        {
            int exitCode = 0;
            
            try
            {
                args = ServiceDebugUtils.WaitForRemoteDebuggerAttach(args);
#if DEBUG
                const string releaseVersion = "(Debug version)";
#else
                const string releaseVersion = "";
#endif
                Logger.LogInfo(LoggingEvents.InfoEvent.ServiceStart(AppInfo.Name, AppInfo.Version, AppInfo.ExecutablePath, releaseVersion));

                var exchangeStreamingConfig = Config.GetExchangeConfigData(args);

                var dbContextFactory = new ClientDbEntitiesFactory();

                using (var exchangeStreamService = new StreamingManager(dbContextFactory, exchangeStreamingConfig))
                {

                    if (Environment.UserInteractive)
                    {
                        try
                        {
                            var service = new ExchangeListenerService(exchangeStreamService);

                            service.StartService(args);
                            ServiceDebugUtils.WaitForEscKeyToContinue();
                            service.StopService();
                            exitCode = 0;
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, LoggingEvents.ErrorEvent.ExceptionWhenStartingTheService("Console"));
                            Console.WriteLine("Exception thrown");
                            Console.WriteLine(ExceptionUtils.ExceptionToStringMessage(ex));
                            exitCode = 9;
                        }
                    }
                    else
                    {
                        try
                        {
                            var servicesToRun = new ServiceBase[]
                            {
                                new ExchangeListenerService(exchangeStreamService)
                            };
                            ServiceBase.Run(servicesToRun);
                            exitCode = 0;
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, LoggingEvents.ErrorEvent.ExceptionWhenStartingTheService("Service"));
                            exitCode = 10;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, LoggingEvents.ErrorEvent.ExceptionInTheInitialSetupOfTheService());
                exitCode = 11;
            }

            Logger.LogInfo(LoggingEvents.InfoEvent.ServiceStop(exitCode));
            return exitCode;
        }
    }
}
