using System;
using System.ServiceProcess;
using PlannerCalendarClient.DataAccess;
using PlannerCalendarClient.Logging;
using PlannerCalendarClient.Utility;

namespace PlannerCalendarClient.PlannerCommunicatorService
{
    partial class PlannerCommunicatorService : ServiceBase
    {
        private static readonly ILogger Logger = Logging.Logger.GetLogger();

        private DailyCallbackTimer _resourceUpdateTimer;
        private DailyCallbackTimer _calendarEventFetchTimer;
        private IntervalCallbackTimer _calendarEventUpdateTimer;

        private readonly IClientDbEntitiesFactory _dbContextFactory;
        private readonly ServiceConfiguration _serviceConfiguration;

        public enum commands
        {
            RunFullPlannerCalendarFetch = 253,
            RunFullPlannerResourceFetch = 252
        }

        public PlannerCommunicatorService(IClientDbEntitiesFactory dbContextFactory, ServiceConfiguration serviceConfiguration)
        {
            InitializeComponent();

            // For parallel call to PlannerExternalCalendar service.
            System.Net.ServicePointManager.DefaultConnectionLimit = 20;

            _dbContextFactory = dbContextFactory;

            _serviceConfiguration = serviceConfiguration;

            // These Flags set whether or not to handle that specific
            // type of event. Set to true if you need it, false otherwise.
            this.CanHandlePowerEvent = false;
            this.CanHandleSessionChangeEvent = false;
            this.CanPauseAndContinue = false;
            this.CanShutdown = true;
            this.CanStop = true;
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                base.OnStart(args);

                StartResourceUpdater(_dbContextFactory, _serviceConfiguration);
                StartCalendarSynchronizer(_dbContextFactory, _serviceConfiguration);
                StartCalendarEventUpdater(_dbContextFactory, _serviceConfiguration);
            }
            catch (Exception ex)
            {
                var serviceStartException = LoggingEvents.ErrorEvent.ServiceStartException();
                ExitCode = serviceStartException.EventId;
                Logger.LogError(ex, serviceStartException);

                throw;
            }
        }

        protected override void OnStop()
        {
            InternalOnStop();
            base.OnStop();
        }

        protected override void OnShutdown()
        {
            InternalOnStop();
            base.OnShutdown();
        }

        protected override void OnCustomCommand(int command)
        {
            base.OnCustomCommand(command);

            switch ((commands)command)
            {
                case commands.RunFullPlannerCalendarFetch:
                    try
                    {
                        Logger.LogInfo(LoggingEvents.InfoEvent.RunServiceCommand(command, ((commands)command).ToString()));
                        CalendarSynchronizer.ServiceProcessing(_dbContextFactory, _serviceConfiguration);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(LoggingEvents.ErrorEvent.ErrorRunningServiceCommand(command, ((commands)command).ToString()), ex);
                    }
                    break;

                case commands.RunFullPlannerResourceFetch:
                    try
                    {
                        Logger.LogInfo(LoggingEvents.InfoEvent.RunServiceCommand(command, ((commands)command).ToString()));
                        ResourceUpdater.ServiceProcessing(_dbContextFactory, _serviceConfiguration);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(LoggingEvents.ErrorEvent.ErrorRunningServiceCommand(command, ((commands)command).ToString()), ex);
                    }
                    break;

                default:
                    // write an error in the log. Unknown command
                    Logger.LogError(LoggingEvents.ErrorEvent.UnknownServiceCommand(command));
                    break;
            }
        }


        private void StartCalendarEventUpdater(IClientDbEntitiesFactory dbContextFactory, ServiceConfiguration serviceConfiguration)
        {
            Action calendarEventUpdateAction = () => CalendarUpdater.ServiceProcessing(dbContextFactory, serviceConfiguration);
            _calendarEventUpdateTimer = new IntervalCallbackTimer(
                calendarEventUpdateAction,
                serviceConfiguration.CalendarEventUpdateInterval, 
                "Planner Calendar Event Updater", 
                autoRestart: true);
            _calendarEventUpdateTimer.Start();
        }

        private void StartCalendarSynchronizer(IClientDbEntitiesFactory dbContextFactory, ServiceConfiguration serviceConfiguration)
        {
            Action calendarEventFetchAction = () => CalendarSynchronizer.ServiceProcessing(dbContextFactory, serviceConfiguration);

            if (Properties.Settings.Default.UpdateCalendarAtStartup)
            {
                calendarEventFetchAction.Invoke();
            }

            _calendarEventFetchTimer = new DailyCallbackTimer(calendarEventFetchAction,
                serviceConfiguration.CalendarEventFetchSchedule, "Planner Calendar Synchronizer", autoRestart: true);
            _calendarEventFetchTimer.Start();
        }

        private void StartResourceUpdater(IClientDbEntitiesFactory dbContextFactory, ServiceConfiguration serviceConfiguration)
        {
            Action resourceUpdaterAction = () => ResourceUpdater.ServiceProcessing(dbContextFactory, serviceConfiguration);

            if (Properties.Settings.Default.UpdateResourcesAtStartup)
            {
                resourceUpdaterAction.Invoke();
            }

            _resourceUpdateTimer = new DailyCallbackTimer(resourceUpdaterAction, 
                serviceConfiguration.ResourceUpdateSchedule, "Planner Resource Updater", autoRestart: true);
            _resourceUpdateTimer.Start();
        }

        private void InternalOnStop()
        {
            // Stop the timers
            _resourceUpdateTimer.Stop();
            _calendarEventFetchTimer.Stop();
            _calendarEventUpdateTimer.Stop();
        }

        /// <summary>
        /// Use to start service from program main 
        /// </summary>
        public void StartService()
        {
            OnStart(new string[0]);
        }

        /// <summary>
        /// Use to stop service from program main
        /// </summary>
        public void StopService()
        {
            OnStop();
        }
    }
}