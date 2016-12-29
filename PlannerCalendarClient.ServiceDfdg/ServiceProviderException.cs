using System;

namespace PlannerCalendarClient.ServiceDfdg
{
    [Serializable]
    public class ServiceProviderException : Exception
    {
        public ServiceProviderException() { }
        public ServiceProviderException(string message) : base(message) { }
        public ServiceProviderException(string message, Exception innerException) : base(message, innerException) { }
    }
}
