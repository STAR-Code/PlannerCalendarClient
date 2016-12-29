namespace PlannerCalendarClient.Logging
{
    public abstract class EventIdBase
    {
        protected EventIdBase(ushort eventId, string message, EventType eventType)
        {
            EventId = eventId;
            Message = message;
            EventType = eventType;
        }

        // EventLog has a limit on the EventId of ushort.MaxValue (65535)
        public ushort EventId { get; private set; }
        public string Message { get; private set; }
        public EventType EventType { get; private set; }


        public override string ToString()
        {
            return string.Format("[{0}:{1}]", EventType.ToString(), EventId);
        }
    }

    public abstract class ErrorEventIdBase : EventIdBase
    {
        protected ErrorEventIdBase(ushort eventId, string message)
            : base(eventId, message, EventType.Error)
        {
        }
    }

    public abstract class WarningEventIdBase : EventIdBase
    {
        protected WarningEventIdBase(ushort eventId, string message)
            : base(eventId, message, EventType.Warning)
        {
        }
    }

    public abstract class InfoEventIdBase : EventIdBase
    {
        protected InfoEventIdBase(ushort eventId, string message)
            : base(eventId, message, EventType.Info)
        {
        }
    }

    public abstract class DebugEventIdBase : EventIdBase
    {
        protected DebugEventIdBase(ushort eventId, string message)
            : base(eventId, message, EventType.Debug)
        {
        }
    }

    public enum EventType
    {
        Info,
        Warning,
        Error,
        Debug
    }

    // EventLog has a limit on the EventId of ushort.MaxValue (65535)
    public enum EventIdRangeStart : ushort
    {
        ExchangeListenerService = 1000,
        EventProcessorService = 2000,
        PlannerCommunicatorService = 3000,
        ExchangeStreamingService = 4000,
        ExchangeServiceCreator = 5000,
        ServiceDfdg = 6000,
        Utility = 7000,
        DataAccess = 8000,
        Logging = 9000,
    }
}