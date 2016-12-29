using PlannerCalendarClient.DataAccess;
using PlannerCalendarClient.Logging;
using System;
using System.ServiceProcess;
using PlannerCalendarClient.Utility;

namespace PlannerCalendarClient.EventProcessorService
{
    class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static int Main(string[] args)
        {
            int exitCode = 0;

            var logger = Logging.Logger.GetLogger();
            try
            {
#if DEBUG
                const string releaseVersion = "(Debug version)";
#else
                const string releaseVersion = "";
#endif
                logger.LogInfo(LoggingEvents.InfoEvent.ServiceStart(AppInfo.Name, AppInfo.Version, AppInfo.ExecutablePath, releaseVersion));

                var service = new EventProcessorService(new ClientDbEntitiesFactory());

                if (Environment.UserInteractive)
                {
                    try
                    {
                        service.StartService(args);
                        ServiceDebugUtils.WaitForEscKeyToContinue();
                        service.StopService();
                        exitCode = 0;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, LoggingEvents.ErrorEvent.ExceptionThrown());
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
                        logger.LogError(ex, LoggingEvents.ErrorEvent.ExceptionThrown());
                        exitCode = 10;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, LoggingEvents.ErrorEvent.ExceptionThrown());
                exitCode = 11;
            }

            logger.LogInfo(LoggingEvents.InfoEvent.ServiceStop(exitCode));
            return exitCode;
        }
    }
}