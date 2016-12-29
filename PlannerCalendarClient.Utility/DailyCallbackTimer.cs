using System;
using System.Linq;
using System.Threading;
using PlannerCalendarClient.Logging;

namespace PlannerCalendarClient.Utility
{
    public class DailyCallbackTimer : IDisposable
    {
        private static readonly ILogger Logger = Logging.Logger.GetLogger();

        private readonly TimeSpan[] _schedule;
        private readonly string _name;
        private readonly bool _autoRestart;
        private readonly TimeSpan _minPostpone = new TimeSpan(0, 0, 30);

        private Timer _timer;
        private DateTime? _nextActivation;
        private volatile bool _running;
        private volatile bool _waiting;

        private readonly object _lock = new object();

        /// <summary>
        /// Callback timer which fires at the specified times
        /// Must be manually restarted after event
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="schedule"></param>
        /// <param name="name"></param>
        /// /// <param name="autoRestart">Indicates whether the timer should restart after event. Default is false.</param>
        public DailyCallbackTimer(Action callback, TimeSpan[] schedule, string name, bool autoRestart = false)
        {
            if (callback == null) throw new ArgumentNullException("callback");
            if (schedule == null) throw new ArgumentNullException("schedule");
            if (!schedule.Any()) throw new ArgumentOutOfRangeException("schedule", "The schedule list must not be empty");
            if (name == null) throw new ArgumentNullException("name");

            _schedule = schedule;
            _name = name;
            _autoRestart = autoRestart;
            
            _timer = new Timer(LocalTimerCallback, callback, Timeout.Infinite, Timeout.Infinite);
        }

        public void Start()
        {
            lock (_lock)
            {
                Logger.LogDebug(LoggingEvents.DebugEvent.DailyCallbackTimerStart(_name));
                _running = true;
                InternalStartTimer(false);
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                _nextActivation = null;
                _running = false;
                Logger.LogDebug(LoggingEvents.DebugEvent.DailyCallbackTimerStop(_name));
            }
        }

        public bool IsRunning()
        {
            return _running;
        }

        public bool AutoRestart()
        {
            return _autoRestart;
        }

        public DateTime? NextActivation()
        {
            return _nextActivation;
        }

        public bool IsWaiting()
        {
            return _waiting;
        }

        private void InternalStartTimer(bool autoRestart)
        {
            if (!_waiting)
            {
                _nextActivation = GetNextActivation();

                var waitPeriod = _nextActivation - DateTime.Now;

                Logger.LogDebug(LoggingEvents.DebugEvent.DailyCallbackTimeWillRunAt(_name, _nextActivation, waitPeriod, (autoRestart ? "Autorestart" : "Start" )));

                _timer.Change((long)waitPeriod.Value.TotalMilliseconds, Timeout.Infinite);
                _waiting = true;
            }
            else
            {
                Logger.LogError(LoggingEvents.ErrorEvent.ErrorReactivationDailyCallbackTimer(_name));
            }
        }

        private DateTime GetNextActivation()
        {
            DateTime nextActivation;

            // Add a min postpone so that we are not selecting a time in the past.
            var now = DateTime.Now.Add(_minPostpone);

            var orderedSchedule = _schedule.OrderBy(t => t); // Just to be sure
            foreach (var timeSpan in orderedSchedule)
            {
                // Check for next time today
                nextActivation = now.Date.Add(timeSpan);
                if (nextActivation > now)
                    return nextActivation;
            }

            nextActivation = now.AddDays(1).Date.Add(orderedSchedule.First()); // First time tomorrow
            return nextActivation;
        }
        
        private void LocalTimerCallback(object state)
        {
            Logger.LogDebug(LoggingEvents.DebugEvent.DailyCallbackTimerCallback(_name));

            // 2015-06-24 OLBH  Add the try/catch block. 
            // A exception must not be propagate through this method.
            // This metode is run by the timer callback and must not throw exception because it can kill the process.
            try
            {
                _waiting = false;
                var action = (Action)state;
                action();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, LoggingEvents.ErrorEvent.DailyCallbackTimerCallbackException(_name));
            }

            if (_autoRestart && _running) // Only restart if stop hasn't been signalled
            {
                Logger.LogDebug(LoggingEvents.DebugEvent.DailyCallbackTimerRestart(_name));
                lock (_lock)
                {
                    InternalStartTimer(true);
                }
            }
            else
            {
                Logger.LogDebug(LoggingEvents.DebugEvent.DailyCallbackTimerNoRestart(_name));
                _running = false;
            }
        }

        public void Dispose()
        {
            Stop();
            _timer.Dispose();
            _timer = null;
        }
    }
}