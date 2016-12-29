using System;
using System.Collections.Generic;
using EWS = Microsoft.Exchange.WebServices.Data;

namespace PlannerCalendarClient.EventProcessorService
{
    internal interface IExchangeGateway
    {
        /// <summary>
        /// Get a singe appointment by it exchange id.
        /// </summary>
        /// <param name="ewsId"></param>
        /// <returns></returns>
        IAppointmentEx GetAppointment(string ewsId);
        /// <summary>
        /// Get a mailbox's appointments in the specified period.
        /// </summary>
        /// <param name="mailAddress"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        IEnumerable<IAppointmentEx> GetAppointmentFromExchangeCalendarView(string mailAddress, DateTime startDate, DateTime endDate);
        /// <summary>
        /// Get the given occurrence of a reoccurrence appointment.
        /// </summary>
        /// <param name="masterAppointment">The master appointment of the reoccurrence</param>
        /// <param name="i">The appointment index</param>
        /// <returns></returns>
        IAppointmentEx GetAppointmentOccurrence(IAppointmentEx masterAppointment, int i);

        /// <summary>
        /// Convert 
        /// </summary>
        /// <param name="masterAppointment"></param>
        /// <param name="ewsDeleteAppointment"></param>
        /// <returns></returns>
        IAppointmentEx ConvertDeleteReoccurrenceAppointment(IAppointmentEx masterAppointment, EWS.DeletedOccurrenceInfo ewsDeleteAppointment);
    }
}