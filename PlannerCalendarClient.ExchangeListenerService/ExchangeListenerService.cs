using System;
using System.ServiceProcess;
using PlannerCalendarClient.ExchangeStreamingService;
using PlannerCalendarClient.Logging;

namespace PlannerCalendarClient.ExchangeListenerService
{
    public partial class ExchangeListenerService : ServiceBase
    {
        private static readonly ILogger Logger = Logging.Logger.GetLogger();

        private readonly StreamingManager _subscriber = null;

        public enum commands
        {
           RunMailAutodiscover = 255
        }

        public ExchangeListenerService()
        {
            // These Flags set whether or not to handle that specific
            // type of event. Set to true if you need it, false otherwise.
            this.CanHandlePowerEvent = false;
            this.CanHandleSessionChangeEvent = false;
            this.CanPauseAndContinue = false;
            this.CanShutdown = true;
            this.CanStop = true;
        }

        public ExchangeListenerService(StreamingManager subscriber)
        {
            _subscriber = subscriber;
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                _subscriber.Start();
                base.OnStart(args);
            }
            catch (Exception ex)
            {
                var evt = LoggingEvents.ErrorEvent.ExceptionInOnStart();
                // The 1064 is the windows error message:  ERROR_EXCEPTION_IN_SERVICE 
                ExitCode = evt.EventId;
                Logger.LogError(ex, evt);
                throw;
            }

        }

        protected override void OnStop()
        {
            _subscriber.Stop();
            base.OnStop();
        }

        protected override void OnShutdown()
        {
            _subscriber.Stop(); ;
            base.OnShutdown();
        }


        protected override void OnCustomCommand(int command)
        {
              base.OnCustomCommand(command);

              switch ((commands)command)
              {
                  case commands.RunMailAutodiscover:
                      try
                      {
                          Logger.LogInfo(LoggingEvents.InfoEvent.RunServiceCommand(command, ((commands)command).ToString()));
                          _subscriber.ForceUpdateMailAccounts();
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

        /// <summary>
        /// public Start method method so that it is possible to run it as a console application.
        /// </summary>
        public void StartService(string[] args)
        {
            OnStart(args);
        }

        /// <summary>
        /// public Stop method method so that it is possible to run it as a console application.
        /// </summary>
        public void StopService()
        {
            OnStop();
        }
    }
}