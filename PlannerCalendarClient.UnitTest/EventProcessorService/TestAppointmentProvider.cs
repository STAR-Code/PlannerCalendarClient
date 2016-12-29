using PlannerCalendarClient.EventProcessorService;
using System;
using System.Collections.Generic;

namespace PlannerCalendarClient.UnitTest.EventProcessorService
{
    internal class TestAppointmentProvider : IAppointmentProvider
    {
        public static Appointment Appointment1 { get; set; }
        public static Appointment Appointment2 { get; set; }
        public static Appointment Appointment3 { get; set; }
        public static Appointment Appointment4 { get; set; }
        public static Appointment DeletedAppointment1 { get; set; }
        public static Appointment CancelledAppointment1 { get; set; }
        public static Appointment FreeAppointment1 {get;set;}

        public static void InitializeTestData()
        {
            Appointment1 = new Appointment
            {
                ICalUid = "1",
                Start = DateTime.Today.AddDays(5).AddHours(10),
                End = DateTime.Today.AddDays(5).AddHours(11),
                IsCancelled = false,
                IsDeleted = false,
                IsRecurring = false,
                IsFree = false,
                EmailAddress = "user3@Exchjcp.com"
            };
            Appointment2 = new Appointment
            {
                ICalUid = "2",
                Start = DateTime.Today.AddDays(4).AddHours(12),
                End = DateTime.Today.AddDays(4).AddHours(12).AddMinutes(30),
                IsCancelled = false,
                IsDeleted = false,
                IsRecurring = false,
                IsFree = false,
                EmailAddress = "user3@Exchjcp.com"
            };
            Appointment3 = new Appointment
            {
                ICalUid = "3",
                Start = DateTime.Today.AddDays(5).AddHours(10),
                End = DateTime.Today.AddDays(5).AddHours(11),
                IsCancelled = false,
                IsDeleted = false,
                IsRecurring = false,
                IsFree = false,
                EmailAddress = "user3@Exchjcp.com"
            };
            Appointment4 = new Appointment
            {
                ICalUid = "4",
                Start = DateTime.Today.AddDays(5).AddHours(14).AddMinutes(15),
                End = DateTime.Today.AddDays(5).AddHours(15).AddMinutes(15),
                IsCancelled = false,
                IsDeleted = false,
                IsRecurring = false,
                IsFree = false,
                EmailAddress = "user3@Exchjcp.com"
            };
            DeletedAppointment1 = new Appointment
            {
                ICalUid = "11",
                Start = DateTime.Today.AddDays(5).AddHours(14).AddMinutes(15),
                End = DateTime.Today.AddDays(5).AddHours(15).AddMinutes(15),
                IsCancelled = false,
                IsDeleted = true,
                IsRecurring = false,
                IsFree = false,
                EmailAddress = "user3@Exchjcp.com"
            };
            CancelledAppointment1 = new Appointment
            {
                ICalUid = "21",
                Start = DateTime.Today.AddDays(5).AddHours(14).AddMinutes(15),
                End = DateTime.Today.AddDays(5).AddHours(15).AddMinutes(15),
                IsCancelled = true,
                IsDeleted = false,
                IsRecurring = false,
                IsFree = false,
                EmailAddress = "user3@Exchjcp.com"
            };
            FreeAppointment1 = new Appointment
            {
                ICalUid = "31",
                Start = DateTime.Today.AddDays(5).AddHours(14).AddMinutes(15),
                End = DateTime.Today.AddDays(5).AddHours(15).AddMinutes(15),
                IsCancelled = false,
                IsDeleted = false,
                IsRecurring = false,
                IsFree = true,
                EmailAddress = "user3@Exchjcp.com"
            };
        }

        public IEnumerable<IAppointment> GetAppointmentsById(string id, DateTime start, DateTime end)
        {
            switch (id)
            {
                case "1": return OnList(Appointment1);
                case "2": return OnList(Appointment2);
                case "3": return OnList(Appointment3);
                case "4": return OnList(Appointment4);
                case "11": return OnList(DeletedAppointment1);
                case "21": return OnList(CancelledAppointment1);
                case "31": return OnList(FreeAppointment1);
                default:
                    return OnList(Appointment4);
            }
        }

        private IEnumerable<IAppointment> OnList(IAppointment app)
        {
            return new List<IAppointment> { app };
        }

        public IEnumerable<IAppointment> GetAppointmentsByMailbox(string mailBox, DateTime startDate, DateTime endDate)
        {
            throw new NotImplementedException();
        }
    }
}
