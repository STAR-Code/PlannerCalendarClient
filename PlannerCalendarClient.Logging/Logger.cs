using log4net;
using System;
using System.Diagnostics;
using System.Reflection;

namespace PlannerCalendarClient.Logging
{
    public class Logger : ILogger
    {
        private readonly ILog _logger;
        private readonly string _assemblyName;

        private Logger(Type type)
        {
            _logger = LogManager.GetLogger(type);
            var assembly = Assembly.GetEntryAssembly();
            if (assembly != null)
                _assemblyName = assembly.GetName().Name; // This is the nicest format :-)
            else
                _assemblyName = "Unknown";
        }

        private Logger(string name)
        {
            _logger = LogManager.GetLogger(name);
            var assembly = Assembly.GetEntryAssembly();
            if (assembly != null)
                _assemblyName = assembly.GetName().Name; // This is the nicest format :-)
            else
                _assemblyName = "Unknown";
        }

        /// <summary>
        /// Retrieves or creates a named logger
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static ILogger GetLogger(string name)
        {
            var logger = new Logger(name);

            if (LogManager.GetRepository().Configured == false)
            {
                log4net.Config.XmlConfigurator.Configure();
                logger.LogDebug(DebugEvent.LoggerConfigured, name);
            }

            return logger;
        }

        /// <summary>
        /// Retrieves or creates a logger.
        /// The full name of calling the type will be used as the name of the logger.
        /// </summary>
        /// <returns></returns>
        public static ILogger GetLogger()
        {
            // Get the type that called this method  
            var frame = new StackFrame(1, false);
            var declaringType = frame.GetMethod().DeclaringType;

            var logger = new Logger(declaringType);

            if (LogManager.GetRepository().Configured == false)
            {
                log4net.Config.XmlConfigurator.Configure();
                logger.LogDebug(DebugEvent.LoggerConfigured, declaringType.AssemblyQualifiedName);
            }

            return logger;
        }

        public void LogError(EventIdBase logEvent, params object[] data)
        {
            ThreadContext.Properties["EventID"] = (int)logEvent.EventId;
            ThreadContext.Properties["AppName"] = _assemblyName;
            var msg = logEvent.Message.SafeFormat(data);
            _logger.Error(msg);
        }

        public void LogError(Exception exception, EventIdBase logEvent, params object[] data)
        {
            ThreadContext.Properties["EventID"] = (int)logEvent.EventId;
            ThreadContext.Properties["AppName"] = _assemblyName;
            var msg = logEvent.Message.SafeFormat(data);
            _logger.Error(msg, exception);
        }

        public void LogWarning(WarningEventIdBase logEvent, params object[] data)
        {
            ThreadContext.Properties["EventID"] = (int)logEvent.EventId;
            ThreadContext.Properties["AppName"] = _assemblyName;
            var msg = logEvent.Message.SafeFormat(data);
            _logger.Warn(msg);
        }

        public void LogWarning(Exception exception, WarningEventIdBase logEvent, params object[] data)
        {
            ThreadContext.Properties["EventID"] = (int)logEvent.EventId;
            ThreadContext.Properties["AppName"] = _assemblyName;
            var msg = logEvent.Message.SafeFormat(data);
            _logger.Warn(msg, exception);
        }

        public void LogInfo(InfoEventIdBase logEvent, params object[] data)
        {
            ThreadContext.Properties["EventID"] = (int)logEvent.EventId;
            ThreadContext.Properties["AppName"] = _assemblyName;
            var msg = logEvent.Message.SafeFormat(data);
            _logger.InfoFormat(msg);
        }

        public void LogDebug(DebugEventIdBase logEvent, params object[] data)
        {
            ThreadContext.Properties["EventID"] = (int)logEvent.EventId;
            ThreadContext.Properties["AppName"] = _assemblyName;
            var msg = logEvent.Message.SafeFormat(data);
            _logger.Debug(msg);
        }
    }

    internal class DebugEvent : DebugEventIdBase
    {
        private const ushort RangeStart = (ushort)EventIdRangeStart.Logging;

        private DebugEvent(ushort eventId, string message)
            : base(eventId, message)
        { }

        internal static DebugEventIdBase LoggerConfigured
        {
            get { return new DebugEvent(RangeStart + 1, "Logger is configured '{0}'."); }
        }
    }
}

