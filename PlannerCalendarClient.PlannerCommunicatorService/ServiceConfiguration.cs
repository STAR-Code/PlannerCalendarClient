using System;
using System.Configuration;
using System.Linq;
using PlannerCalendarClient.Logging;

namespace PlannerCalendarClient.PlannerCommunicatorService
{
    internal class ServiceConfiguration
    {
        private static readonly ILogger Logger = Logging.Logger.GetLogger();

        public string JobcenterNumber;
        public string RequestUserIdentifier;
        public string ConnectionString;
        public TimeSpan[] ResourceUpdateSchedule;
        public int MaxCalendarEventUpdatesPerCall;
        public int MaxCalendarEventFetchesPerCall;
        public int SimultaniousCalls;
        public TimeSpan[] CalendarEventFetchSchedule;
        public TimeSpan CalendarEventUpdateInterval;
        public int CalendarEventsPeriod;

        public ServiceConfiguration()
        {
            SetJobcenterNumber();
            SetRequestUserIdentifier();
            SetResourceUpdateSchedule();
            SetConnectionString();
            SetMaxCalendarEventUpdatesPerCall();
            SetMaxCalendarEventFetchesPerCall();
            SetCalendarEventUpdateInterval();
            SetCalendarEventFetchSchedule();
            SetCalendarEventsPeriod();
            SetSimultaniousCalls();
        }

        private void SetSimultaniousCalls()
        {
            SimultaniousCalls = Properties.Settings.Default.SimultaniousCalls;
            if (SimultaniousCalls > 10)
            {
                Logger.LogInfo(LoggingEvents.InfoEvent.ConfigurationInfo(string.Format("The Planner Communicator does not permit more than 10 parallel calls to Planner. Number of simultanious calls are reset to 10")));
                SimultaniousCalls = 10;
            }
            else
                Logger.LogInfo(LoggingEvents.InfoEvent.ConfigurationInfo(string.Format("Number of simultanious calls to Planner: {0}", SimultaniousCalls)));
        }

        private void SetJobcenterNumber()
        {
            JobcenterNumber = Properties.Settings.Default.JobcenterNumber;
            if (string.IsNullOrWhiteSpace(JobcenterNumber))
            {
                throw new ConfigurationErrorsException("Configuration JobcenterNumber is not found!");
            }
            Logger.LogInfo(LoggingEvents.InfoEvent.ConfigurationInfo(string.Format("Jobcenter number for the service: {0}.", JobcenterNumber)));
        }

        private void SetRequestUserIdentifier()
        {
            RequestUserIdentifier = Properties.Settings.Default.RequestUserIdentifier;
            if (string.IsNullOrWhiteSpace(RequestUserIdentifier))
            {
                throw new ConfigurationErrorsException("Configuration RequestUserIdentifier is not found!");
            }
            Logger.LogInfo(LoggingEvents.InfoEvent.ConfigurationInfo(string.Format("The user identifier used when calling the service: {0}.", RequestUserIdentifier)));
        }

        private void SetResourceUpdateSchedule()
        {
            var temp = Properties.Settings.Default.ResourceUpdateSchedule;
            if (string.IsNullOrWhiteSpace(temp))
            {
                throw new ConfigurationErrorsException("Configuration ResourceUpdateSchedule is not found!");
            }
            Logger.LogInfo(LoggingEvents.InfoEvent.ConfigurationInfo(string.Format("Hours for refreshing Resource-list for the service: {0}.", temp))); 
            ResourceUpdateSchedule = temp.Split(';').Select(x => TimeSpan.Parse(string.Format("{0}:00", x))).ToArray();
        }

        private void SetConnectionString()
        {
            var conStr = ConfigurationManager.ConnectionStrings["ECSClientExchangeDbEntities"];
            if (conStr == null)
            {
                throw new ConfigurationErrorsException("Configuration ECSClientExchangeDbEntities is not found!");
            }
            ConnectionString = conStr.ConnectionString;
        }

        private void SetCalendarEventFetchSchedule()
        {
            var temp = Properties.Settings.Default.CalendarSynchronizationSchedule;
            if (string.IsNullOrWhiteSpace(temp))
            {
                throw new ConfigurationErrorsException("Configuration CalendarEventFetchSchedule is not found!");
            }

            Logger.LogInfo(LoggingEvents.InfoEvent.ConfigurationInfo(string.Format("CalendarEventFetchSchedule: {0}.", temp)));
            CalendarEventFetchSchedule = temp.Split(';').Select(x => TimeSpan.Parse(string.Format("{0}:00", x))).ToArray();
        }

        private void SetMaxCalendarEventUpdatesPerCall()
        {
            int temp;
            try
            {
                temp = Properties.Settings.Default.MaxCalendarEventUpdatesPerCall;
            }
            catch(NullReferenceException)
            {
                throw new ConfigurationErrorsException("Configuration MaxCalendarEventUpdatesPerCall is not found!");
            }

            Logger.LogInfo(LoggingEvents.InfoEvent.ConfigurationInfo(string.Format("The max number of events to send to Planner per call: {0}.", temp)));
            MaxCalendarEventUpdatesPerCall = temp;
        }

        private void SetMaxCalendarEventFetchesPerCall()
        {
            int temp;
            try
            {
                temp = Properties.Settings.Default.MaxCalendarEventFetchesPerCall;
            }
            catch (NullReferenceException)
            {
                throw new ConfigurationErrorsException("Configuration SetMaxCalendarEventFetchesPerCall is not found!");
            }

            Logger.LogInfo(LoggingEvents.InfoEvent.ConfigurationInfo(string.Format("The max number of resources to fetch events for from Planner per call: {0}.", temp)));
            MaxCalendarEventFetchesPerCall = temp;
        }

        private void SetCalendarEventUpdateInterval()
        {
            var temp = Properties.Settings.Default.CalendarEventUpdateInterval;
            Logger.LogInfo(LoggingEvents.InfoEvent.ConfigurationInfo(string.Format("Interval for sending calendar events to Planner: {0} s.", temp)));
            CalendarEventUpdateInterval = (temp);
        }

        private void SetCalendarEventsPeriod()
        {
            var temp = Properties.Settings.Default.CalendarEventsPeriodInMonths;
            Logger.LogInfo(LoggingEvents.InfoEvent.ConfigurationInfo(string.Format("Length of the period to fetch events from Planner: {0} months.", temp)));
            CalendarEventsPeriod = temp;
        }
    }
}
