using System;

namespace PlannerCalendarClient.ServiceDfdg
{
    [Serializable]
    public class ServiceProviderOperationException : Exception
    {
        public ServiceProviderOperationException() : base() { }
        public ServiceProviderOperationException(string message) : base(message) { }
        public ServiceProviderOperationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
