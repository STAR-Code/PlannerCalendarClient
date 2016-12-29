using System.ServiceProcess;
using ExternalCalendarService.Client.ServiceLibrary;
using log4net;

namespace ExternalCalendarService.Client.ExchangeListenerService
{
    public partial class ListenerService : ServiceBase 
    {
        private ILog _log = LogManager.GetLogger(typeof(ListenerService));

        private readonly IServiceBase _subscriber = null;

        public ListenerService()
        {
            this.ServiceName = "Planner Exchange Listener Service";
            this.EventLog.Log = "Application";

            _log.InfoFormat("Service name: \"{0}\".", this.ServiceName);
            _log.InfoFormat("Service event log: \"{0}\" source: \"{1}\".", this.EventLog.Log, this.EventLog.Source);

            // These Flags set whether or not to handle that specific
            //  type of event. Set to true if you need it, false otherwise.
            this.CanHandlePowerEvent = false;
            this.CanHandleSessionChangeEvent = false;
            this.CanPauseAndContinue = false;
            this.CanShutdown = true;
            this.CanStop = true;
        }

        public ListenerService(IServiceBase subscriber)
        {
            _subscriber = subscriber;
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _subscriber.Start();
        }

        protected override void OnStop()
        {
            _subscriber.Stop();;
        }
    }
}
