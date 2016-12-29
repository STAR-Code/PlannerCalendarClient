using System;
using System.Collections.Generic;

namespace PlannerCalendarClient.EventProcessorService
{
    internal interface IAppointmentProvider
    {
        IEnumerable<IAppointment> GetAppointmentsById(string ewsId, DateTime startDate, DateTime endDate);

        IEnumerable<IAppointment> GetAppointmentsByMailbox(string mailBox, DateTime startDate, DateTime endDate);
    }
}
