using PlannerCalendarClient.DataAccess;
using PlannerCalendarClient.Logging;
using PlannerCalendarClient.Utility;
using System;
using System.Linq;
using System.ServiceProcess;

namespace PlannerCalendarClient.EventProcessorService
{
    partial class EventProcessorService : ServiceBase
    {
        private static readonly ILogger Logger = Logging.Logger.GetLogger();

        private readonly IClientDbEntitiesFactory entityFactory;
        private IntervalCallbackTimer notificationProcessorTimer;
        private DailyCallbackTimer performFullAppointmentPull;

        public enum commands
        {
            RunFullExchangeCalendarFetch = 254,
        }


        public EventProcessorService(IClientDbEntitiesFactory entityFactory)
        {
            InitializeComponent();
            this.entityFactory = entityFactory;

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

                var serviceConfiguration = new ServiceConfiguration();
                IAppointmentProviderFactory appointmentProviderFactory = new ExchangeAppointmentProviderFactory(args);

                Action notificationProcessorAction = () => EventProcessor.ProcessNotifications(serviceConfiguration, entityFactory, appointmentProviderFactory);
                notificationProcessorTimer = new IntervalCallbackTimer(notificationProcessorAction, serviceConfiguration.NotificationProcessingInterval, "Notification Processor", autoRestart: true);

                Action performFullAppointmentPullAction = () => EventProcessor.PerformFullAppointmentPull(serviceConfiguration, entityFactory, appointmentProviderFactory);
                performFullAppointmentPull = new DailyCallbackTimer(performFullAppointmentPullAction, serviceConfiguration.FullAppointmentPullSchedule, "Full Appointment Pull", autoRestart: true);

                if (serviceConfiguration.MakeFullCalendarPullAtStartup)
                {
                    // Start full pull - and start timers when done
                    performFullAppointmentPullAction.BeginInvoke(new AsyncCallback((a) =>
                    {
                        notificationProcessorTimer.Start();
                        performFullAppointmentPull.Start();
                    }), null);
                }
                else
                {
                    notificationProcessorTimer.Start();
                    performFullAppointmentPull.Start();
                }
            }
            catch (Exception ex)
            {
                var evt = LoggingEvents.ErrorEvent.ServiceStartException();
                ExitCode = evt.EventId;
                Logger.LogError(ex, evt);
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
                case commands.RunFullExchangeCalendarFetch:
                    try
                    {
                        Logger.LogInfo(LoggingEvents.InfoEvent.RunServiceCommand(command, ((commands)command).ToString()));
                    }
                    catch (Exception ex)
                    {
                        // Log the error
                        Logger.LogError(LoggingEvents.ErrorEvent.ErrorRunningServiceCommand(command, ((commands)command).ToString()), ex);
                    }
                    break;

                default:
                    // write an error in the log. Unknown command
                    Logger.LogError(LoggingEvents.ErrorEvent.UnknownServiceCommand(command));
                    break;
            }
        }

        private void InternalOnStop()
        {
            // Stop the timers
            notificationProcessorTimer.Stop();
            performFullAppointmentPull.Stop();
        }

        public void StartService(string[] args)
        {
            OnStart(args);
        }

        public void StopService()
        {
            OnStop();
        }
    }
}
