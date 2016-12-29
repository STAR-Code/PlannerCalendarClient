using System;
using System.ServiceProcess;
using PlannerCalendarClient.DataAccess;
using PlannerCalendarClient.Logging;
using PlannerCalendarClient.Utility;

namespace PlannerCalendarClient.PlannerCommunicatorService
{
    class Program
    {
        private static readonly ILogger Logger = Logging.Logger.GetLogger();

        static int Main(string[] args)
        {
            int exitCode = 0;

            try
            {
#if DEBUG
                const string releaseVersion = "(Debug version)";
#else
                const string releaseVersion = "";
#endif
                Logger.LogInfo(LoggingEvents.InfoEvent.ServiceStart(AppInfo.Name, AppInfo.Version, AppInfo.ExecutablePath, releaseVersion));

                var dbContextFactory = new ClientDbEntitiesFactory();

                var serviceConfiguration = new ServiceConfiguration();

                var service = new PlannerCommunicatorService(dbContextFactory, serviceConfiguration);

                if (Environment.UserInteractive)
                {
                    try
                    {
                        service.StartService();
                        ServiceDebugUtils.WaitForEscKeyToContinue();
                        service.StopService();
                        exitCode = 0;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, LoggingEvents.ErrorEvent.ServiceAsConsoleRunException());
                        Console.WriteLine("Console Application ended with an exception.");
                        Console.WriteLine(ExceptionUtils.ExceptionToStringMessage(ex));
                        exitCode = 9;
                    }
                }
                else
                {
                    try
                    {
                        ServiceBase.Run(service);
                        exitCode = 0;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, LoggingEvents.ErrorEvent.ServiceBaseRunException());
                        exitCode = 10;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, LoggingEvents.ErrorEvent.ServiceCreateException());
                exitCode = 11;
            }

            Logger.LogInfo(LoggingEvents.InfoEvent.ServiceStop(exitCode));
            return exitCode;
        }
    }
}