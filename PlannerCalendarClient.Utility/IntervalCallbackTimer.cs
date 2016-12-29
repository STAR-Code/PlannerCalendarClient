using System;
using System.Threading;
using PlannerCalendarClient.Logging;

namespace PlannerCalendarClient.Utility
{
    public class IntervalCallbackTimer : IDisposable
    {
        private static readonly ILogger Logger = Logging.Logger.GetLogger();

        private readonly TimeSpan _callbackInterval = new TimeSpan(0, 0, 0,-1);
        private readonly string _name;
        private readonly bool _autoRestart;

        private readonly Timer _timer;
        private DateTime? _nextActivation;
        private volatile bool _running;
        private volatile bool _waiting;

        private readonly object _lock = new object();

        /// <summary>
        /// Callback timer which fires after specified interval
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="callbackInterval"></param>
        /// <param name="name"></param>
        /// <param name="autoRestart">Indicates whether the timer should restart after event. Default is false.</param>
        public IntervalCallbackTimer(Action callback, TimeSpan callbackInterval, string name, bool autoRestart)
        {
            if (callback == null) throw new ArgumentNullException("callback");
            if (callbackInterval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException("callbackInterval", "Callback interval must not be negative or zero");
            if (name == null) throw new ArgumentNullException("name");

            _callbackInterval = callbackInterval;
            _name = name;
            _autoRestart = autoRestart;

            _timer = new Timer(LocalTimerCallback, callback, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// StartNow call the callback action within a second and then start the timer with 
        /// the requested interval.
        /// </summary>
        public void StartNow()
        {
            lock (_lock)
            {
                Logger.LogDebug(LoggingEvents.DebugEvent.IntervalCallbackTimerStart(_name));
                _running = true;
                InternalStartTimer(new TimeSpan(0, 0, 1), false);
            }
        }

        /// <summary>
        /// Start the timer. The first event will be at the specified interval.
        /// </summary>
        public void Start()
        {
            lock (_lock)
            {
                Logger.LogDebug(LoggingEvents.DebugEvent.IntervalCallbackTimerStart(_name));
                _running = true;
                InternalStartTimer(_callbackInterval, false);
            }
        }

        /// <summary>
        // Stop the timer.
        /// </summary>
        public void Stop()
        {
            lock (_lock)
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                _nextActivation = null;
                _running = false;
                Logger.LogDebug(LoggingEvents.DebugEvent.IntervalCallbackTimerStop(_name));
            }
        }

        public bool IsRunning()
        {
            return _running;
        }

        /// <summary>
        /// The autorestart state.
        /// If true the timer is automatically reenable when the callback method return.
        /// </summary>
        /// <returns></returns>
        public bool AutoRestart()
        {
            return _autoRestart;
        }

        /// <summary>
        /// Return the timestamp for the next callback
        /// </summary>
        /// <returns></returns>
        public DateTime? NextActivation()
        {
            return _nextActivation;
        }

        /// <summary>
        /// Return true if the state is waiting for the timer callback
        /// </summary>
        /// <returns></returns>
        public bool IsWaiting()
        {
            return _waiting;
        }

        private void InternalStartTimer(TimeSpan callbackTimeInterval, bool autoRestart)
        {
            if (!_waiting)
            {
                _nextActivation = DateTime.Now.Add(callbackTimeInterval);
                Logger.LogDebug(LoggingEvents.DebugEvent.IntervalCallbackTimerCallbackWillRunAt(_name, _nextActivation, callbackTimeInterval, (autoRestart ? "Autorestart" : "Start" )));
                _timer.Change((long)callbackTimeInterval.TotalMilliseconds, Timeout.Infinite);
                _waiting = true;
            }
            else
            {
                Logger.LogError(LoggingEvents.ErrorEvent.ErrorReactivationIntervalCallbackTimer(_name));
            }
        }

        private void LocalTimerCallback(object state)
        {
                Logger.LogDebug(LoggingEvents.DebugEvent.IntervalCallbackTimerCallback(_name));

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
                    Logger.LogError(ex, LoggingEvents.ErrorEvent.IntervalCallbackTimerCallbackException(_name));
                }

                if (_autoRestart && _running) // Only restart if stop hasn't been signalled
                {
                    Logger.LogDebug(LoggingEvents.DebugEvent.IntervalCallbackTimerRestart(_name));
                    lock (_lock)
                    {
                        InternalStartTimer(_callbackInterval, true);
                    }
                }
                else
                {
                    Logger.LogDebug(LoggingEvents.DebugEvent.IntervalCallbackTimerNoRestart(_name));
                    _running = false;
                }
        }

        public void Dispose()
        {
            Stop();
            _timer.Dispose();
        }
    }
}