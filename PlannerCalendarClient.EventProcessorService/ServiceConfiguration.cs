using System;
using System.Linq;
using PlannerCalendarClient.Logging;
using System.Configuration;

namespace PlannerCalendarClient.EventProcessorService
{
    internal class ServiceConfiguration
    {
        private static readonly ILogger Logger = Logging.Logger.GetLogger();

        public readonly TimeSpan NotificationProcessingInterval;
        public readonly int SimultaniousCalls;
        public readonly int CalendarEventsPeriodInMonths;
        public readonly bool MakeFullCalendarPullAtStartup;
        public TimeSpan[] FullAppointmentPullSchedule;

        public ServiceConfiguration()
        {
            CalendarEventsPeriodInMonths = Properties.Settings.Default.CalendarEventsPeriodInMonths;
            Logger.LogInfo(LoggingEvents.InfoEvent.ConfigurationInfo("Get calendar event in the next {0} months.", CalendarEventsPeriodInMonths));

            NotificationProcessingInterval = Properties.Settings.Default.NotificationProcessingInterval;
            Logger.LogInfo(LoggingEvents.InfoEvent.ConfigurationInfo("Will check for notifications every {0} seconds.", NotificationProcessingInterval));

            SimultaniousCalls = Properties.Settings.Default.SimultaniousCalls;
            if (SimultaniousCalls > 1)
            {
                Logger.LogInfo(LoggingEvents.InfoEvent.ConfigurationInfo("The Exchange Appointment Provider does currently not support parallel calls to Exchange. Calls to Exchange will be performed one at a time"));
                SimultaniousCalls = 1;
            }
            else
                Logger.LogInfo(LoggingEvents.InfoEvent.ConfigurationInfo("Number of simultanious calls to Exchange: {0}", SimultaniousCalls));

            MakeFullCalendarPullAtStartup = Properties.Settings.Default.MakeFullCalendarPullAtStartup;
            Logger.LogInfo(LoggingEvents.InfoEvent.ConfigurationInfo("Make full calendar pull at startup: {0}", MakeFullCalendarPullAtStartup));

            SetFullAppointmentPullSchedule();
        }

        private void SetFullAppointmentPullSchedule()
        {
            var temp = Properties.Settings.Default.FullAppointmentPullSchedule;
            if (string.IsNullOrWhiteSpace(temp))
            {
                throw new ConfigurationErrorsException("Configuration FullAppointmentPullSchedule is not found!");
            }
            Logger.LogInfo(LoggingEvents.InfoEvent.ConfigurationInfo("The FullAppointmentPullSchedule: {0}.", temp));
            try
            {
                FullAppointmentPullSchedule = temp.Split(';')
                    .Select(x => TimeSpan.Parse(string.Format("{0}:00", x)))
                    .ToArray();
            }
            catch(Exception ex)
            {
                throw new ConfigurationErrorsException(string.Format("Configuration FullAppointmentPullSchedule is malformed: {0}", temp), ex);
            }
        }
    }
}
