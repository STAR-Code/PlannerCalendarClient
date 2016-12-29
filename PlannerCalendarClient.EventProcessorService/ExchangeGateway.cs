using PlannerCalendarClient.ExchangeServiceCreator;
using PlannerCalendarClient.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using EWS = Microsoft.Exchange.WebServices.Data;

namespace PlannerCalendarClient.EventProcessorService
{
    class ExchangeGateway : IExchangeGateway
    {
        private static readonly Logging.ILogger Logger = Logging.Logger.GetLogger();

        #region Exchange proeprty sets

        private static class ExchangePropertySets
        {
            internal readonly static EWS.PropertySet AppointmentPropertiesToRetrieve = new EWS.PropertySet(
                EWS.ItemSchema.Id,
                EWS.AppointmentSchema.Start,
                EWS.AppointmentSchema.End,
                EWS.AppointmentSchema.ICalUid,
                EWS.AppointmentSchema.ICalRecurrenceId,
                EWS.AppointmentSchema.AppointmentType,
                EWS.AppointmentSchema.LegacyFreeBusyStatus,
                EWS.AppointmentSchema.IsCancelled,
                EWS.AppointmentSchema.IsRecurring,
                EWS.ItemSchema.ParentFolderId,

                EWS.AppointmentSchema.Recurrence,
                EWS.AppointmentSchema.FirstOccurrence,
                EWS.AppointmentSchema.LastOccurrence,
                EWS.AppointmentSchema.ModifiedOccurrences,
                EWS.AppointmentSchema.DeletedOccurrences
                );

            internal readonly static EWS.PropertySet AppointmentPropertiesToRetrieveWhenRecurrendIdFails = new EWS.PropertySet(
                EWS.ItemSchema.Id,
                EWS.AppointmentSchema.ICalUid,
                EWS.AppointmentSchema.ICalRecurrenceId
                );

            internal readonly static EWS.PropertySet FolderPropertiesToRetrieve = new EWS.PropertySet(
                EWS.FolderSchema.WellKnownFolderName
                );

            internal readonly static EWS.PropertySet ExchangePropertiesForGetAppointmentByMailbox = new EWS.PropertySet(
                EWS.ItemSchema.Id,
                EWS.AppointmentSchema.Start,
                EWS.AppointmentSchema.End,
                EWS.AppointmentSchema.ICalUid,
                EWS.AppointmentSchema.ICalRecurrenceId,
                EWS.AppointmentSchema.AppointmentType,
                EWS.AppointmentSchema.LegacyFreeBusyStatus,
                EWS.AppointmentSchema.IsCancelled,
                EWS.AppointmentSchema.IsRecurring
                );
        }

        #endregion Exchange proeprty sets

        private readonly EWS.ExchangeService _exchangeService;
        private readonly string _serviceUserMailbox;
        private const string CalendarEventCalIdConcatString = "$";
        private readonly bool _useImpersonation;

        public ExchangeGateway(EWS.ExchangeService exchangeService, string serviceUserMailbox, bool useImpersonation)
        {
            _exchangeService = exchangeService;
            _serviceUserMailbox = serviceUserMailbox;
            _useImpersonation = useImpersonation;
        }

        /// <summary>
        /// Use in the flow for appointment notification
        /// </summary>
        /// <param name="ewsId">exchange appointment id</param>
        /// <returns></returns>
        public IAppointmentEx GetAppointment(string ewsId)
        {
            var mailbox = ResolveAppointmentMailbox(ewsId, _serviceUserMailbox);

            Logger.LogDebug(LoggingEvents.DebugEvent.CallExchangeAppointmentBind("GetAppointment"));

            var exchAppointment = ExchangeServerUtils.CallImpersonated(
                _exchangeService,
                mailbox,
                _useImpersonation,
                (exchService) => EWS.Appointment.Bind(exchService, ewsId, ExchangePropertySets.AppointmentPropertiesToRetrieve),
                "Appointment.Bind");

            EWS.WellKnownFolderName? appointmentFolderName = null;
            if (exchAppointment.ParentFolderId != null)
            {
                Logger.LogDebug(LoggingEvents.DebugEvent.CallExchangeFolderBind("GetAppointment (ParentFolderId is missing)"));
                var appointmentFolder = ExchangeServerUtils.CallImpersonated<EWS.Folder>(
                    _exchangeService,
                    mailbox,
                    _useImpersonation,
                    (exchService) => EWS.Folder.Bind(exchService, exchAppointment.ParentFolderId, ExchangePropertySets.FolderPropertiesToRetrieve),
                    "Folder.Bind");

                    appointmentFolderName = appointmentFolder.WellKnownFolderName;
            }

            IAppointmentEx appointment = ConvertAppointment(exchAppointment, mailbox, appointmentFolderName ?? EWS.WellKnownFolderName.Calendar);

            return appointment;
        }
        
        /// <summary>
        /// Use in the flow for "get all appointments"
        /// <param name="mailBox"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IAppointmentEx> GetAppointmentFromExchangeCalendarView(string mailBox, DateTime startDate, DateTime endDate)
        {
            // get the user/mailbox's specific folder for the calendar items.
            EWS.CalendarFolder calendarFolder = ExchangeFolderUtils.GetMailAccountsCalendarFolder(_exchangeService, mailBox, _useImpersonation);

            var cView = new EWS.CalendarView(startDate, endDate);
            cView.PropertySet = ExchangePropertySets.ExchangePropertiesForGetAppointmentByMailbox;

            Logger.LogDebug(LoggingEvents.DebugEvent.CallExchangeFindAppointments("GetAppointmentFromExchangeCalendarView"));

            var exchAppointments = ExchangeServerUtils.CallImpersonated<EWS.FindItemsResults<EWS.Appointment>>(
                _exchangeService, 
                mailBox,
                _useImpersonation,
                (exchService) => exchService.FindAppointments(calendarFolder.Id, cView),
                "ExchangeService.FindAppointments");
            
            var appointments = new List<IAppointmentEx>(exchAppointments.TotalCount);
            foreach (var exchAppointment in exchAppointments)
            {
                try
                {
                    // This method fix an error in Exchange. 
                    // Fix problem where exchange don't return the reoccurrence id for an appointment occurrence.
                    EWS.Appointment exchAppointment2;
                    if (exchAppointment.IsRecurring && exchAppointment.ICalRecurrenceId == null)
                    {
                        exchAppointment2 = FixMissingReoccurrenceAppointmentProperties(exchAppointment, mailBox);
                    }
                    else
                    {
                        exchAppointment2 = exchAppointment;
                    }

                    var appointment = ConvertAppointment(
                        exchAppointment2, 
                        mailBox,
                        calendarFolder.WellKnownFolderName ?? EWS.WellKnownFolderName.Calendar);

                    appointments.Add(appointment);
                }
                catch (ExchangeBaseException ex)
                {
                   Logger.LogWarning(ex, LoggingEvents.WarningEvent.AppointmentCouldNotBeExtractedFromExchangeBecauseOfCalId(mailBox, exchAppointment.Id.UniqueId, exchAppointment.Start, exchAppointment.End, ex));
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(LoggingEvents.WarningEvent.AppointmentCouldNotBeExtractedFromExchangeBecauseOfCalId(mailBox, exchAppointment.Id.UniqueId, exchAppointment.Start, exchAppointment.End, ex));
                }
            }

            return appointments;
        }

        /// <summary>
        /// This method fix an error in Exchange. 
        /// The method is called for reoccurrence appointment that miss the ICalRecurrenceId property (The property is null).
        /// </summary>
        /// <param name="reoccurrentAppointment"></param>
        /// <param name="mailbox"></param>
        private EWS.Appointment FixMissingReoccurrenceAppointmentProperties(EWS.Appointment reoccurrentAppointment, string mailbox)
        {
            Logger.LogDebug(LoggingEvents.DebugEvent.CallExchangeAppointmentBind("GetReoccurrenceAppointmentProperty"));

            EWS.Appointment ewsAppointment = ExchangeServerUtils.CallImpersonated<EWS.Appointment>(
                _exchangeService,
                mailbox,
                _useImpersonation,
                (exchService) =>
                    EWS.Appointment.Bind(
                        exchService, 
                        reoccurrentAppointment.Id.UniqueId,
                        ExchangePropertySets.ExchangePropertiesForGetAppointmentByMailbox),
                    "Appointment.Bind (FixMissingReoccurrenceAppointmentProperty)");

            return ewsAppointment;
        }

        public IAppointmentEx GetAppointmentOccurrence(IAppointmentEx masterAppointment, int occurrenceIndex)
        {
            Logger.LogDebug(LoggingEvents.DebugEvent.CallExchangeAppointmentBind("GetAppointmentOccurrence"));

            var exchAppointment = ExchangeServerUtils.CallImpersonated(
                _exchangeService,
                masterAppointment.EmailAddress,
                _useImpersonation,
                (exchService) =>
                    EWS.Appointment.BindToOccurrence(
                        exchService,
                        masterAppointment.UniqueId,
                        occurrenceIndex, 
                        ExchangePropertySets.AppointmentPropertiesToRetrieve),
                "Appointment.BindToOccurrence");

            return ConvertAppointment(exchAppointment, masterAppointment.EmailAddress, masterAppointment.WellKnownFolderName);
        }

        public IAppointmentEx ConvertDeleteReoccurrenceAppointment(IAppointmentEx masterAppointment, EWS.DeletedOccurrenceInfo ewsDeleteAppointment)
        {
            // QUERK: IsCancelled is not being returned from calendar.FindAppointments()
            var appointmentEx = new AppointmentEx
            {
                ICalUid = CreateReoccurrenceCalId(masterAppointment.UniqueId, masterAppointment.ICalUid, ewsDeleteAppointment.OriginalStart),
                Start = ewsDeleteAppointment.OriginalStart,
                End = ewsDeleteAppointment.OriginalStart + (masterAppointment.End - masterAppointment.Start),
                EmailAddress = masterAppointment.EmailAddress,
                IsCancelled = true,
                IsDeleted = true,
                IsRecurring = true,
                IsFree = true,
                UniqueId = masterAppointment.UniqueId,
                LegacyFreeBusyStatus = masterAppointment.LegacyFreeBusyStatus,
                ICalRecurrenceId = ewsDeleteAppointment.OriginalStart,
                WellKnownFolderName = EWS.WellKnownFolderName.DeletedItems
            };

            return appointmentEx;
        }

        private IAppointmentEx ConvertAppointment(EWS.Appointment ewsAppointment, string mailbox, EWS.WellKnownFolderName appointmentFolderName)
        {
            // QUERK: IsCancelled is not being returned from calendar.FindAppointments()
            bool isCancelled = false;
            EWS.Recurrence recurrence = null;
            DateTime? icalRecurrenceId = null;
            EWS.OccurrenceInfoCollection modifiedOccurrences = null;
            EWS.DeletedOccurrenceInfoCollection deletedOccurrences = null;

            ewsAppointment.TryGetProperty<bool>(EWS.AppointmentSchema.IsCancelled, out isCancelled);
            ewsAppointment.TryGetProperty<DateTime?>(EWS.AppointmentSchema.ICalRecurrenceId, out icalRecurrenceId);
            ewsAppointment.TryGetProperty<EWS.Recurrence>(EWS.AppointmentSchema.Recurrence, out recurrence);
            ewsAppointment.TryGetProperty<EWS.OccurrenceInfoCollection>(EWS.AppointmentSchema.ModifiedOccurrences, out modifiedOccurrences);
            ewsAppointment.TryGetProperty<EWS.DeletedOccurrenceInfoCollection>(EWS.AppointmentSchema.DeletedOccurrences, out deletedOccurrences);
            var appointmentEx = new AppointmentEx
            {
                ICalUid = AppointmentICalUidHelper(ewsAppointment),
                Start = ewsAppointment.Start,
                End = ewsAppointment.End,
                EmailAddress = mailbox,
                IsCancelled = isCancelled,
                IsDeleted = IsAppointmentInDeletedItemsFolder(appointmentFolderName),
                IsRecurring = IsRecurringAppointment(ewsAppointment),
                IsFree = (ewsAppointment.LegacyFreeBusyStatus == EWS.LegacyFreeBusyStatus.Free),
                // AppointmentEx property
                UniqueId = ewsAppointment.Id.UniqueId,
                LegacyFreeBusyStatus = ewsAppointment.LegacyFreeBusyStatus,
                Recurrence = recurrence,
                ICalRecurrenceId = icalRecurrenceId,
                ModifiedOccurrences = modifiedOccurrences,
                DeletedOccurrences = deletedOccurrences,
                // No an ews.property
                WellKnownFolderName = appointmentFolderName,
            };

            return appointmentEx;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ewsId">exchange appointment id</param>
        /// <param name="serviceUserMailbox"></param>
        /// <returns></returns>
        private string ResolveAppointmentMailbox(string ewsId, string serviceUserMailbox)
        {
            var aiRequest = new EWS.AlternateId
            {
                UniqueId = ewsId,
                Mailbox = serviceUserMailbox,
                Format = EWS.IdFormat.EwsId
            };
            try
            {
                // No impersonation call is use here.
                Logger.LogDebug(LoggingEvents.DebugEvent.CallExchangeConvertId("ResolveAppointmentMailbox"));
                var response = (EWS.AlternateId)_exchangeService.ConvertId(aiRequest, EWS.IdFormat.StoreId);
                return response.Mailbox;
            }
            catch (Exception ex)
            {
                throw new ExchangeMailboxNotFoundException(ewsId, serviceUserMailbox, ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="appointment"></param>
        /// <returns></returns>
        private string AppointmentICalUidHelper(EWS.Appointment appointment)
        {
            //if ICalUid is null, try to use exchange object Id
            if (string.IsNullOrWhiteSpace(appointment.ICalUid))
            {
                throw new ExchangeAppointmentCalIdEmptyException(LoggingEvents.ErrorEvent.ErrorExchangeAppointmentCalIdEmptyException(appointment.Id.UniqueId));
            }

            if (IsRecurringOccurrenceAppointment(appointment))
            {
                return CreateReoccurrenceCalId(appointment.Id, appointment.ICalUid, appointment.ICalRecurrenceId);
            }
            else
            {
                return appointment.ICalUid;
            }
        }

        private bool IsRecurringOccurrenceAppointment(EWS.Appointment appointment)
        {
            return appointment.AppointmentType == EWS.AppointmentType.Occurrence
                   || appointment.AppointmentType == EWS.AppointmentType.Exception;
        }

        private bool IsRecurringAppointment(EWS.Appointment appointment)
        {
            return appointment.AppointmentType == EWS.AppointmentType.Occurrence
                   || appointment.AppointmentType == EWS.AppointmentType.Exception
                   || appointment.AppointmentType == EWS.AppointmentType.RecurringMaster;
        }

        private static string CreateReoccurrenceCalId(EWS.ItemId id, string calId, DateTime? iCalRecurrenceId)
        {
            if (!iCalRecurrenceId.HasValue)
            {
                throw new ExchangeAppointmentCalIdEmptyException(LoggingEvents.ErrorEvent.ErrorExchangeAppointmentCalRecurrenceIdEmptyException(id.UniqueId, calId));
            }

            var dateTimeToInvariantString = iCalRecurrenceId.Value.ToString("yyyyMMdd-HHmmss");
            return string.Concat(calId, CalendarEventCalIdConcatString, dateTimeToInvariantString);
        }


        private readonly static EWS.WellKnownFolderName[] deletedItemsFolderNames = 
            {
                EWS.WellKnownFolderName.DeletedItems,
                EWS.WellKnownFolderName.ArchiveDeletedItems,
                EWS.WellKnownFolderName.ArchiveRecoverableItemsDeletions,
                EWS.WellKnownFolderName.RecoverableItemsDeletions
            };

        internal static bool IsAppointmentInDeletedItemsFolder(EWS.WellKnownFolderName? wellKnownFolderName)
        {
            // ASSUMPTION: The ewsAppointment is said to be deleted, if it resides in one of the wellknown recycle bin folders
            return wellKnownFolderName.HasValue && deletedItemsFolderNames.Contains(wellKnownFolderName.Value);
        }
    }
}