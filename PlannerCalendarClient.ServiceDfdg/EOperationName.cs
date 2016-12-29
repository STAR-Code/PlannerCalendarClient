namespace PlannerCalendarClient.ServiceDfdg
{
    /// <summary>
    /// An enum value for all the method calls that the service support, except GetSystemLog
    /// The type is used in the ServiceLogMessage. 
    /// </summary>
    public enum EOperationName
    {
        None,
        CreateEvents,
        UpdateEvents,
        DeleteEvents,
        GetEvents,
        GetResources,
    }
}
