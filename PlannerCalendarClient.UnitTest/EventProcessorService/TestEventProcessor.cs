using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlannerCalendarClient.DataAccess;
using PlannerCalendarClient.EventProcessorService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PlannerCalendarClient.UnitTest.EventProcessorService
{
    [TestClass]
    public class TestEventProcessor
    {
        TestEntityFactory entityFactory;
        TestEntities db;
        DateTime today10days = DateTime.Today.AddDays(10);
        IAppointmentProviderFactory appointmentProviderFactory = new TestAppointmentProviderFactory();
        ServiceConfiguration serviceConfiguration = new ServiceConfiguration();
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        const string testMail = "test@test.dk";

        [TestInitialize]
        public void SetupTest()
        {
            db = new TestEntities();
            entityFactory = new TestEntityFactory(db);
            TestAppointmentProvider.InitializeTestData();
        }


        [TestMethod]
        public void ProcessNotifications_Create()
        {
            // Arrange
            var appointment = TestAppointmentProvider.Appointment2;
            db.Notifications.Add(new Notification { EwsId = appointment.ICalUid, EwsTimestamp = DateTime.Now.AddMinutes(-1) });

            // Act
            cancellationTokenSource.Cancel(); // Stop the process after first iteration
            EventProcessor.ProcessNotifications(serviceConfiguration, entityFactory, appointmentProviderFactory);

            // Assert
            Assert.IsFalse(db.Notifications.Any(), "The notification was not removed from the Notification table");
            Assert.IsTrue(db.NotificationLogs.Any(), "The notification was not moved to the NotificationLog table");

            var calendarEvent = db.CalendarEvents.SingleOrDefault();
            Assert.IsTrue(calendarEvent != null, "No CalendarEvent was created");
            Assert.AreEqual(appointment.ICalUid, calendarEvent.CalId, "ICal Id missmatch");
            Assert.AreEqual(appointment.IsDeleted, calendarEvent.IsDeleted, "The calendar event should not be marked as deleted");

            var syncLog = db.SyncLogs.SingleOrDefault();
            Assert.IsNotNull(syncLog, "No SyncLog was created");
            Assert.AreEqual(appointment.Start, syncLog.CalendarStart, "Start date is incorrect");
            Assert.AreEqual(appointment.End, syncLog.CalendarEnd, "End date is incorrect");
            Assert.AreEqual(DataAccess.Constants.SyncLogOperationCREATE, syncLog.Operation, "Sync operation is incorrect");
        }

        [TestMethod]
        public void ProcessNotifications_Update()
        {
            // Arrange
            var appointment = TestAppointmentProvider.Appointment1;
            // Trigger the update by an notification
            db.Notifications.Add(new Notification { EwsId = appointment.ICalUid, EwsTimestamp = DateTime.Now.AddMinutes(-1) });
            // Setup database state for the appointment 
            var calendarEvent = new CalendarEvent { Id = 1, CalId = appointment.ICalUid, MailAddress = appointment.EmailAddress, IsDeleted = false };
            db.CalendarEvents.Add(calendarEvent);
            calendarEvent.AddSyncLog(db, appointment.Start, appointment.End.AddMinutes(15), Constants.SyncLogOperationCREATE);

            // Act
            cancellationTokenSource.Cancel(); // Stop the process after first iteration
            EventProcessor.ProcessNotifications(serviceConfiguration, entityFactory, appointmentProviderFactory);

            // Assert
            Assert.AreEqual(2, db.SyncLogs.Count(), "There should have been two logs in SyncLog");
            var synclogs = db.SyncLogs.OrderBy(sl => sl.CreatedDate).ToArray();
            Assert.AreEqual(Constants.SyncLogOperationCREATE, synclogs[0].Operation, "The first operation should have been the create operation");
            Assert.AreEqual(Constants.SyncLogOperationUPDATE, synclogs[1].Operation, "The second operation should have been the update operation");
            Assert.AreEqual(appointment.Start, synclogs[1].CalendarStart, "Incorrect start time");
            Assert.AreEqual(appointment.End, synclogs[1].CalendarEnd, "Incorrect end time - was not updated");

            var calendarEvent2 = db.CalendarEvents.SingleOrDefault();
            Assert.IsNotNull(calendarEvent2);
            Assert.AreEqual(false, calendarEvent2.IsDeleted, "The calendar event should not have been deleted");
        }

        [TestMethod]
        public void ProcessNotifications_Cancelled()
        {
            // Arrange
            var appointment = TestAppointmentProvider.CancelledAppointment1;
            // Trigger the update by an notification
            db.Notifications.Add(new Notification { EwsId = appointment.ICalUid, EwsTimestamp = DateTime.Now.AddMinutes(-1) });
            // Setup database state for the appointment 
            var calendarEvent = new CalendarEvent { Id = 1, CalId = appointment.ICalUid, MailAddress = appointment.EmailAddress, IsDeleted = false };
            db.CalendarEvents.Add(calendarEvent);
            calendarEvent.AddSyncLog(db, appointment.Start, appointment.End, Constants.SyncLogOperationCREATE);

            // Act
            cancellationTokenSource.Cancel(); // Stop the process after first iteration
            EventProcessor.ProcessNotifications(serviceConfiguration, entityFactory, appointmentProviderFactory);

            // Assert
            Assert.AreEqual(2, db.SyncLogs.Count(), "There should have been two logs in SyncLog");
            var synclogs = db.SyncLogs.OrderBy(sl => sl.CreatedDate).ToArray();
            Assert.AreEqual(Constants.SyncLogOperationDELETE, synclogs[1].Operation, "The second operation should have been the delete operation");

            var calendarEvent2 = db.CalendarEvents.SingleOrDefault();
            Assert.IsNotNull(calendarEvent2);
            Assert.AreEqual(true, calendarEvent2.IsDeleted, "The calendar event should have been deleted");
        }

        [TestMethod]
        public void ProcessNotifications_Delete()
        {
            // Arrange
            var appointment = TestAppointmentProvider.DeletedAppointment1;
            // Trigger the update by an notification
            db.Notifications.Add(new Notification { EwsId = appointment.ICalUid, EwsTimestamp = DateTime.Now.AddMinutes(-1) });
            // Setup database state for the appointment 
            var calendarEvent = new CalendarEvent { Id = 1, CalId = appointment.ICalUid, MailAddress = appointment.EmailAddress, IsDeleted = false };
            db.CalendarEvents.Add(calendarEvent);
            calendarEvent.AddSyncLog(db, appointment.Start, appointment.End.AddMinutes(15), Constants.SyncLogOperationCREATE);
            calendarEvent.AddSyncLog(db, appointment.Start, appointment.End, Constants.SyncLogOperationUPDATE);

            // Act
            cancellationTokenSource.Cancel(); // Stop the process after first iteration
            EventProcessor.ProcessNotifications(serviceConfiguration, entityFactory, appointmentProviderFactory);

            // Assert
            Assert.AreEqual(3, db.SyncLogs.Count(), "There should have been three logs in SyncLog");
            var synclogs = db.SyncLogs.OrderBy(sl => sl.CreatedDate).ToArray();
            Assert.AreEqual(Constants.SyncLogOperationDELETE, synclogs[2].Operation, "The third operation should have been the delete operation");

            var calendarEvent2 = db.CalendarEvents.SingleOrDefault();
            Assert.IsNotNull(calendarEvent2);
            Assert.AreEqual(true, calendarEvent2.IsDeleted, "The calendar event should have been deleted");
        }


        [TestMethod]
        public void DoesAppointmentQualifyForUpdate_EmptySyncLog_NoCalendarEvent()
        {
            // Arrange
            var appointment = TestAppointmentProvider.Appointment1;
            string operation;
            long? calendarEventId;

            // Act
            var result = EventProcessor.DoesAppointmentQualifyForUpdate(db, appointment, out operation, out calendarEventId);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(DataAccess.Constants.SyncLogOperationCREATE, operation);
        }

        [TestMethod]
        public void DoesAppointmentQualifyForUpdate_AppointmentUnchanged()
        {
            // Arrange
            var start = today10days.AddHours(10);
            var end = today10days.AddHours(11);
            var calId = "IcalId1";

            var appointment = new Appointment
            {
                ICalUid = "IcalId1",
                EmailAddress = testMail,
                Start = start,
                End = end,
                IsCancelled = false
            };
            string operation;
            long? calendarEventId;

            // Arrange database
            var calendarEvent = db.CalendarEvents.Add(new CalendarEvent { CalId = calId, MailAddress = testMail, IsDeleted = false });
            calendarEvent.AddSyncLog(db, start, end, Constants.SyncLogOperationCREATE);

            // Act
            var result = EventProcessor.DoesAppointmentQualifyForUpdate(db, appointment, out operation, out calendarEventId);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void DoesAppointmentQualifyForUpdate_AppointmentChanged()
        {
            // Arrange
            var start = today10days.AddHours(10);
            var end = today10days.AddHours(11);
            var calId = "IcalId1";

            var appointment = new Appointment
            {
                ICalUid = "IcalId1",
                EmailAddress = testMail,
                Start = start,
                End = end.AddMinutes(15),   // Extended with 15 minutes
                IsCancelled = false
            };
            string operation;
            long? calendarEventId;

            // Arrange database
            var calendarEvent = db.CalendarEvents.Add(new CalendarEvent { CalId = calId, MailAddress = testMail, IsDeleted = false });
            calendarEvent.AddSyncLog(db, start, end, Constants.SyncLogOperationCREATE);

            // Act
            var result = EventProcessor.DoesAppointmentQualifyForUpdate(db, appointment, out operation, out calendarEventId);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(Constants.SyncLogOperationUPDATE, operation);
        }

        [TestMethod]
        public void DoesAppointmentQualifyForUpdate_AppointmentCancelled()
        {
            // Arrange
            var start = today10days.AddHours(10);
            var end = today10days.AddHours(11);
            var calId = "IcalId1";

            var appointment = new Appointment
            {
                EmailAddress = testMail,
                ICalUid = "IcalId1",
                Start = start,
                End = end,
                IsCancelled = true,      // Cancelled
                IsDeleted = false
            };
            string operation;
            long? calendarEventId;

            // Arrange database
            var calendarEvent = db.CalendarEvents.Add(new CalendarEvent { CalId = calId, MailAddress = testMail, IsDeleted = false });
            calendarEvent.AddSyncLog(db, start, end, Constants.SyncLogOperationCREATE);

            // Act
            var result = EventProcessor.DoesAppointmentQualifyForUpdate(db, appointment, out operation, out calendarEventId);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(Constants.SyncLogOperationDELETE, operation);
        }

        [TestMethod]
        public void DoesAppointmentQualifyForUpdate_AppointmentDeleted()
        {
            // Arrange
            var start = today10days.AddHours(10);
            var end = today10days.AddHours(11);
            var calId = "IcalId1";

            var appointment = new Appointment
            {
                ICalUid = "IcalId1",
                EmailAddress = testMail,
                Start = start,
                End = end,
                IsCancelled = false,
                IsDeleted = true      // Deleted
            };
            string operation;
            long? calendarEventId;

            // Arrange database
            var calendarEvent = db.CalendarEvents.Add(new CalendarEvent { CalId = calId, MailAddress = testMail, IsDeleted = false });
            calendarEvent.AddSyncLog(db, start, end, Constants.SyncLogOperationCREATE);

            // Act
            var result = EventProcessor.DoesAppointmentQualifyForUpdate(db, appointment, out operation, out calendarEventId);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(Constants.SyncLogOperationDELETE, operation);
        }

        [TestMethod]
        public void DoesAppointmentQualifyForUpdate_DoNotCreateSyncForAppointmentWithSameStartEnd()
        {
            // Arrange
            var start = today10days.AddHours(10);
            var end = today10days.AddHours(10);
            var calId = "IcalId1";

            var appointment = new Appointment
            {
                ICalUid = "IcalId1",
                EmailAddress = testMail,
                Start = start,
                End = end,
                IsCancelled = false,
                IsDeleted = false
            };
            string operation;
            long? calendarEventId;

            // Arrange database
            var calendarEvent = db.CalendarEvents.Add(new CalendarEvent { CalId = calId, MailAddress = testMail, IsDeleted = false });
            calendarEvent.AddSyncLog(db, start, end, Constants.SyncLogOperationCREATE);

            // Act
            var result = EventProcessor.DoesAppointmentQualifyForUpdate(db, appointment, out operation, out calendarEventId);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void DoesAppointmentQualifyForUpdate_DoNotCreateSyncForFreeAppointment()
        {
            // Arrange
            var appointment = TestAppointmentProvider.FreeAppointment1;

            // Act
            string operation;
            long? calendarEventId;
            var result = EventProcessor.DoesAppointmentQualifyForUpdate(db, appointment, out operation, out calendarEventId);

            // Assert
            Assert.IsFalse(result, "This change should not qualify for synchronization");
        }

        [TestMethod]
        public void DoesAppointmentQualifyForUpdate_AppointmentChangedToFree()
        {
            // Arrange
            var appointment = TestAppointmentProvider.FreeAppointment1;
            // Arrange database
            var calendarEvent = db.CalendarEvents.Add(new CalendarEvent { CalId = appointment.ICalUid, MailAddress = appointment.EmailAddress, IsDeleted = false });
            calendarEvent.AddSyncLog(db, appointment.Start, appointment.End, Constants.SyncLogOperationCREATE);

            // Act
            string operation;
            long? calendarEventId;
            var result = EventProcessor.DoesAppointmentQualifyForUpdate(db, appointment, out operation, out calendarEventId);

            // Assert
            Assert.IsTrue(result, "This change should qualify for synchronization");
            Assert.AreEqual(Constants.SyncLogOperationDELETE, operation, "A delete operation was expected");
        }

        [TestMethod]
        public void DoesAppointmentQualifyForUpdate_AppointmentChangedFromBusyToFreeToBusy()
        {
            // Arrange
            var appointment = TestAppointmentProvider.Appointment4;
            // Arrange database
            var calendarEvent = db.CalendarEvents.Add(new CalendarEvent { CalId = appointment.ICalUid, MailAddress = appointment.EmailAddress, IsDeleted = true });
            calendarEvent.AddSyncLog(db, appointment.Start, appointment.End, Constants.SyncLogOperationCREATE, DateTime.Now.AddMinutes(-10));
            calendarEvent.AddSyncLog(db, appointment.Start, appointment.End, Constants.SyncLogOperationDELETE, DateTime.Now);

            // Act
            string operation;
            long? calendarEventId;
            var result = EventProcessor.DoesAppointmentQualifyForUpdate(db, appointment, out operation, out calendarEventId);

            // Assert
            Assert.IsTrue(result, "This change should qualify for synchronization");
            Assert.AreEqual(Constants.SyncLogOperationCREATE, operation, "A delete operation was expected");
        }

        [TestMethod]
        public void DoesAppointmentQualifyForUpdate_AppointmentChangedFromFree()
        {
            // Arrange
            var appointment = TestAppointmentProvider.Appointment4;

            // Act
            string operation;
            long? calendarEventId;
            var result = EventProcessor.DoesAppointmentQualifyForUpdate(db, appointment, out operation, out calendarEventId);

            // Assert
            Assert.IsTrue(result, "This change should qualify for synchronization");
            Assert.AreEqual(Constants.SyncLogOperationCREATE, operation, "A create operation was expected");
        }

        [TestMethod]
        public void DoesAppointmentQualifyForUpdate_AllDayAppointment()
        {
            // Arrange
            var appointment = TestAppointmentProvider.Appointment1;
            appointment.Start = appointment.Start.Date;
            appointment.End = appointment.End.Date.AddDays(1);

            // Act
            string operation;
            long? calendarEventId;
            var result = EventProcessor.DoesAppointmentQualifyForUpdate(db, appointment, out operation, out calendarEventId);

            // Assert
            Assert.IsTrue(result, "This appointment should qualify for synchronization");
            Assert.AreEqual(operation, Constants.SyncLogOperationCREATE, "A create operation was expected");

        }

        [TestMethod]
        public void DoesAppointmentQualifyForUpdate_DoNotSyncCreateToday()
        {
            // Arrange
            var appointment = TestAppointmentProvider.Appointment1;
            appointment.Start = DateTime.Now;
            appointment.End = appointment.Start.AddHours(1);

            // Act
            string operation;
            long? calendarEventId;
            var result = EventProcessor.DoesAppointmentQualifyForUpdate(db, appointment, out operation, out calendarEventId);

            // Assert
            Assert.IsFalse(result, "This appointment should not qualify for synchronization");
        }

        [TestMethod]
        public void DoesAppointmentQualifyForUpdate_DoSyncUpdateToday()
        {
            // Arrange
            var appointment = TestAppointmentProvider.Appointment1;
            appointment.Start = DateTime.Now;
            appointment.End = appointment.Start.AddHours(1);

            var calendarEvent = db.CalendarEvents.Add(new CalendarEvent { CalId = appointment.ICalUid, MailAddress = appointment.EmailAddress });
            calendarEvent.AddSyncLog(db, appointment.Start.AddDays(1), appointment.End.AddDays(1), Constants.SyncLogOperationCREATE);

            // Act
            string operation;
            long? calendarEventId;
            var result = EventProcessor.DoesAppointmentQualifyForUpdate(db, appointment, out operation, out calendarEventId);

            // Assert
            Assert.IsTrue(result, "This appointment should qualify for synchronization");
        }

        [TestMethod]
        public void DoesAppointmentQualifyForUpdate_DoSyncDeleteToday()
        {
            // Arrange
            var appointment = TestAppointmentProvider.Appointment1;
            appointment.Start = DateTime.Now;
            appointment.End = appointment.Start.AddHours(1);
            appointment.IsDeleted = true;

            var calendarEvent = db.CalendarEvents.Add(new CalendarEvent { CalId = appointment.ICalUid, MailAddress = appointment.EmailAddress });
            calendarEvent.AddSyncLog(db, appointment.Start.AddDays(1), appointment.End.AddDays(1), Constants.SyncLogOperationCREATE);

            // Act
            string operation;
            long? calendarEventId;
            var result = EventProcessor.DoesAppointmentQualifyForUpdate(db, appointment, out operation, out calendarEventId);

            // Assert
            Assert.IsTrue(result, "This appointment should qualify for synchronization");
        }

        [TestMethod]
        public void DoesAppointmentQualifyForUpdate_MultiDayAppointment()
        {
            // Arrange
            var appointment = TestAppointmentProvider.Appointment1;
            appointment.End = appointment.End.AddDays(1);

            // Act
            string operation;
            long? calendarEventId;
            var result = EventProcessor.DoesAppointmentQualifyForUpdate(db, appointment, out operation, out calendarEventId);

            // Assert
            Assert.IsTrue(result, "This appointment should qualify for synchronization");
            Assert.AreEqual(operation, Constants.SyncLogOperationCREATE, "A create operation was expected");
        }


        [TestMethod]
        public void DeleteAppointmentsNotInView_OutsideDateScope()
        {
            // Arrange
            var start = DateTime.Today;
            var end = start.AddDays(7).AddHours(23).AddMinutes(59);
            var mailbox = "test@test.dk";

            // Appointments from Exchange view
            var appointments = new List<Appointment>
            {
                new Appointment { ICalUid = "Test1", EmailAddress = mailbox, Start = start.AddHours(8), End = start.AddHours(9)},
                new Appointment { ICalUid = "Test2", EmailAddress = mailbox, Start = end.AddHours(-9), End = end.AddHours(-8)},
            };

            // Appointments in local database
            db.CalendarEvents
                .Add(new CalendarEvent { Id = 1, CalId = "TestOutside1", MailAddress = mailbox })
                .AddSyncLog(db, start.AddDays(-1).AddHours(8), start.AddDays(-1).AddHours(9), Constants.SyncLogOperationCREATE);
            db.CalendarEvents
                .Add(new CalendarEvent { Id = 2, CalId = "Test1", MailAddress = mailbox })
                .AddSyncLog(db, appointments[0].Start, appointments[0].End, Constants.SyncLogOperationCREATE);
            db.CalendarEvents
                .Add(new CalendarEvent { Id = 2, CalId = "Test2", MailAddress = mailbox })
                .AddSyncLog(db, start.AddDays(1).AddHours(8), start.AddDays(1).AddHours(9), Constants.SyncLogOperationCREATE);
            db.CalendarEvents
                .Add(new CalendarEvent { Id = 3, CalId = "Test3", MailAddress = mailbox })
                .AddSyncLog(db, appointments[1].Start, appointments[1].End, Constants.SyncLogOperationCREATE);
            db.CalendarEvents
                .Add(new CalendarEvent { Id = 4, CalId = "TestOutside2", MailAddress = mailbox })
                .AddSyncLog(db, end.AddDays(1).AddHours(8), end.AddDays(1).AddHours(9), Constants.SyncLogOperationCREATE);

            // Act
            EventProcessor.DeleteAppointmentsNotInView(db, start, end, "test@test.dk", appointments);

            // Assert
            var deletedEvents = db.CalendarEvents.Where(ce => ce.IsDeleted);
            Assert.IsFalse(deletedEvents.Any(ce => ce.CalId == "Test1"), "Test1 should not have been marked as deleted");
            Assert.IsFalse(deletedEvents.Any(ce => ce.CalId == "Test2"), "Test2 should not have been marked as deleted");
            Assert.IsFalse(deletedEvents.Any(ce => ce.CalId == "TestOutside1"), "TestOutside1 should not have been marked as deleted");
            Assert.IsFalse(deletedEvents.Any(ce => ce.CalId == "TestOutside2"), "TestOutside2 should not have been marked as deleted");

            Assert.IsTrue(deletedEvents.Any(ce => ce.CalId == "Test3"), "Test3 should have been marked as deleted");
        }
    }
}
