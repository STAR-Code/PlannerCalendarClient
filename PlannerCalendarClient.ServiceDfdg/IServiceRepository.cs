using System;
using System.Collections.Generic;

namespace PlannerCalendarClient.ServiceDfdg
{
    /// <summary>
    /// This interface defines access to the ServiceProvider Repository Webservice (Planner External Calendar Service) 
    /// </summary>
    public interface IServiceRepository
    {
        /// <summary>
        /// Posts creation of calendar-items to the ExternalCalendarService webservice
        /// - note that each item in the list of calendar-items is updated with response from the webservice
        /// </summary>
        /// <param name="calendarEventItems">The calendar items to create</param>
        /// <param name="departmentNumber">The department that is calling the webservice</param>
        /// <param name="requestUserIdentifier"></param>
        /// <returns>The service log response from the service</returns>
        ServiceCallReferenceItem CreateEvents(IEnumerable<CalendarEventItem> calendarEventItems, string departmentNumber, string requestUserIdentifier);

        /// <summary>
        /// Posts updates of calendar-items to the ExternalCalendarService webservice
        /// - note that each item in the list of calendar-items is updated with response from the webservice
        /// </summary>
        /// <param name="calendarEventItems">The calendar items to update</param>
        /// <param name="departmentNumber">The department that is calling the webservice</param>
        /// <param name="requestUserIdentifier"></param>
        /// <returns>The service log response from the service</returns>
        ServiceCallReferenceItem UpdateEvents(IEnumerable<CalendarEventItem> calendarEventItems, string departmentNumber, string requestUserIdentifier);

        /// <summary>
        /// Posts deletion of calendar-items to the ExternalCalendarService webservice
        /// - note that each item in the list of calendar-items is updated with response from the webservice
        /// </summary>
        /// <param name="calendarEventItems">The calendar items to delete</param>
        /// <param name="departmentNumber">The department that is calling the webservice</param>
        /// <param name="requestUserIdentifier"></param>
        /// <returns>The service log response from the service</returns>
        ServiceCallReferenceItem DeleteEvents(IEnumerable<CalendarEventItem> calendarEventItems, string departmentNumber, string requestUserIdentifier);

        /// <summary>
        /// Fetches a list of known (future) calendar items from the ExternalCalendarService webservice
        /// for a given department
        /// </summary>
        /// <param name="departmentNumber">The the department to fetch items for</param>
        /// <param name="requestUserIdentifier"></param>
        /// <param name="mailAddresses">Optional list of mailaddresses to filter the calender-events by</param>
        /// <param name="fromDate"></param>
        /// <param name="toDate"></param>
        /// <param name="serviceCallReferenceItem">Output value: An object with information about the call to the service</param>
        /// <returns>A list of known calender-items registered for a given department</returns>
        IEnumerable<CalendarEventItem> GetEvents(string departmentNumber, string requestUserIdentifier, IEnumerable<string> mailAddresses, DateTime fromDate, DateTime toDate, out ServiceCallReferenceItem serviceCallReferenceItem);

        /// <summary>
        /// Fetches a list of caseworkers or rooms attached to a department
        /// </summary>
        /// <param name="departmentNumber">The department to fetch items for</param>
        /// <param name="requestUserIdentifier"></param>
        /// <param name="mailAddresses">Optional list of mailaddresses to filter the result by</param>
        /// <param name="serviceCallReferenceItem">Output value: An object with information about the call to the service</param>
        /// <returns>A list of resource-items</returns>
        IEnumerable<ResourceItem> GetResources(string departmentNumber, string requestUserIdentifier, IEnumerable<string> mailAddresses, out ServiceCallReferenceItem serviceCallReferenceItem);
    }
}