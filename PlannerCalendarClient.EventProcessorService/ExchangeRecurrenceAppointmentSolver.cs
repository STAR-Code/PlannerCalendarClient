using System;
using System.Collections.Generic;
using Microsoft.Exchange.WebServices.Data;
using PlannerCalendarClient.Logging;

namespace PlannerCalendarClient.EventProcessorService
{
    class ExchangeRecurrenceAppointmentSolver
    {
        private static readonly Logging.ILogger Logger = Logging.Logger.GetLogger();

        private readonly IExchangeGateway _exchangeGateway;

        private readonly List<IAppointmentEx> _emptyResult = new List<IAppointmentEx>();

        private const int EMPTY_INDEX = -1;
        private const int NO_VALUE = 0;
        private const int START_INDEX = 1;

        public ExchangeRecurrenceAppointmentSolver(IExchangeGateway exchangeGateway)
        {
            _exchangeGateway = exchangeGateway;
        }

        public IEnumerable<IAppointmentEx> UnfoldRecurrenceAppoints(IAppointmentEx appointment, DateTime start, DateTime end)
        {
            List<IAppointmentEx> result;

            var startIndex = CalculateRecurringStartIndex(appointment, start, end);
            if (startIndex != EMPTY_INDEX)
            {
                result = GetOccurrencesFromMasterAppointment(appointment, startIndex, start, end);
            }
            else
            {
                // Add the master appointment to get rit of the notification!!!
                result = _emptyResult;
                //result.Add(appointment);
            }

            return result;
        }

        /// <summary>
        /// Find a near optimal startDay index for iterate trought the occurrence appointments.
        /// The optimal startDay index is the first appointment that occurres after the startDay time.
        /// 
        /// From the MSDN documentation for the WS 2.0 API: https://msdn.microsoft.com/en-us/library/office/dd633684(v=exchg.80).aspx
        /// The recurrence pattern defines the frequency of occurrences of items in the recurring series. The following table 
        /// lists the recurrence patterns that you can use to create a recurring series by setting the Recurrence property on 
        /// the Appointment object in the Microsoft Exchange Web Services (EWS) Managed API.
        /// 
        /// Recurrence pattern
        ///Example
        ///- Recurrence.DailyPattern 
        ///  Use to create a series of calendar items that recur daily. Or, use to create a series of calendar items that recur every third day.
        ///- Recurrence.WeeklyPattern 
        ///  Use to create a series of calendar items that recur every Monday. Or, use to create a series of calendar items that recur on Monday, Wednesday, and Friday each week. Or, use to create a series of calendar items that recur on Monday and Tuesday every second week.
        ///- Recurrence.MonthlyPattern 
        ///  Use to create a series of calendar items that recur on the 15th of each month. Or, use to create a series of calendar items that recur on the 15th of every third month.
        ///- Recurrence.RelativeMonthlyPattern 
        ///  Use to create a series of calendar items that recur on the third Monday of each month. Or, use to create a series of calendar items that recur on the third Monday of every fourth month.
        ///- Recurrence.YearlyPattern 
        ///  Use to create a series of calendar items that recur annually on November 15.
        ///- Recurrence.RelativeYearlyPattern 
        ///  Use to create a series of calendar items that recur annually on the last Monday in November.
        /// 
        /// </summary>
        /// <param name="recurringMaster"></param>
        /// <param name="startDay"></param>
        /// <param name="endDay"></param>
        /// <returns></returns>
        private int CalculateRecurringStartIndex(IAppointmentEx recurringMaster, DateTime startDay, DateTime endDay)
        {
            if (recurringMaster.Recurrence == null) throw new ArgumentException("The Recurring Master appointment don't have a Recurrence object (null)");

            ///// Gets an OccurrenceInfo identifying the first occurrence of this meeting.
            //OccurrenceInfo x1 = recurringMaster.FirstOccurrence;
            ///// Gets an OccurrenceInfo identifying the last occurrence of this meeting.
            //OccurrenceInfo x2 = recurringMaster.LastOccurrence;
            ///// Gets a list of modified occurrences for this meeting.
            //OccurrenceInfoCollection x3 = recurringMaster.ModifiedOccurrences;
            ///// Gets a list of deleted occurrences for this meeting.
            //DeletedOccurrenceInfoCollection x4 = recurringMaster.DeletedOccurrences;
            /////
            //LegacyFreeBusyStatus x5 = recurringMaster.LegacyFreeBusyStatus;

            int startIndex = NO_VALUE;

            Recurrence recurrence = recurringMaster.Recurrence;

            Logger.LogDebug(LoggingEvents.DebugEvent.OccurrencePattern(recurrence.GetType().Name, startDay, endDay));

            if (recurrence.EndDate.HasValue && recurrence.EndDate < startDay)
            {
                // The appointments are all before the period we are looking at.
                startIndex = EMPTY_INDEX;
                Logger.LogDebug(LoggingEvents.DebugEvent.OccurrenceFilterInfo(SafeStringFormat.SafeFormat("(== Occurrence startDay date: {0} ) < (period startDay date: {1}). return startDay index: {2}", recurrence.StartDate, startDay, startIndex)));
            }
            else if (recurrence.StartDate > endDay)
            {
                // The appointments are all after the period we are looking at.
                startIndex = EMPTY_INDEX;
                Logger.LogDebug(LoggingEvents.DebugEvent.OccurrenceFilterInfo(SafeStringFormat.SafeFormat("(== Occurrence startDay date: {0}) < (period startDay date: {1}). return startDay index: {2}", recurrence.StartDate, startDay, startIndex)));
            }
            else if (recurrence.StartDate > startDay)
            {
                // The first appointment's startDay date is within the period we are looking at. Start with index 1.
                startIndex = START_INDEX;
                Logger.LogDebug(LoggingEvents.DebugEvent.OccurrenceFilterInfo(SafeStringFormat.SafeFormat("(== Occurrence startDay date: {0}) < (period startDay date: {1}). return startDay index: {2}", recurrence.StartDate, startDay, startIndex)));
            }
            else
            {
                string extraInfo = "";
                int bufferZone = 1;
                var days = (startDay - recurrence.StartDate.Date).Days;

                var intervalPattern = recurrence as Recurrence.IntervalPattern;
                if (intervalPattern != null)
                {
                    if (startIndex == NO_VALUE)
                    {
                        // Daily pattern: a startDay date + a period as a number of dates to the next occurrences.
                        var pattern1 = intervalPattern as Recurrence.DailyPattern; // IntervalPattern
                        if (pattern1 != null)
                        {
                            startIndex = (days / pattern1.Interval) - bufferZone;
                        }
                        else if (startIndex == NO_VALUE)
                        {
                            // NOT in the EWS API 2.0 description 
                            var pattern2 = recurrence as Recurrence.DailyRegenerationPattern; // IntervalPattern
                            if (pattern2 != null)
                            {
                                startIndex = days/pattern2.Interval + 1 - bufferZone;
                                extraInfo = "(NEVER HAPPENT IN EXCHANGE)";
                            }
                            else if (startIndex == NO_VALUE)
                            {
                                // Weekly Pattern: a startDay date + a collection of weekdays + a period as a number of dates from the first occurrence to the next first occurrence.
                                var pattern3 = recurrence as Recurrence.WeeklyPattern; // Intervalpattern
                                if (pattern3 != null)
                                {
                                    //         (antal dage / 7) = uger
                                    //         uger * 'antal aftaler pr. uge' / 'antal uger i intervallet' = antal aftaler (startDay index)
                                    startIndex = ((days/7)*pattern3.DaysOfTheWeek.Count)/pattern3.Interval - bufferZone;
                                    extraInfo = SafeStringFormat.SafeFormat(" NoOfDaysInInterval {0}",pattern3.DaysOfTheWeek.Count);
                                }
                                else if (startIndex == NO_VALUE)
                                {
                                    // NOT in the EWS API 2.0 description 
                                    // .Weekly Regeneration Pattern: a startDay date + period as a number of dates from the first occurrence to the next first occurrence.
                                    var pattern4 = recurrence as Recurrence.WeeklyRegenerationPattern; // Intervalpattern
                                    if (pattern4 != null)
                                    {
                                        startIndex = ((days/7))/pattern4.Interval - bufferZone;
                                    }
                                    else if (startIndex == NO_VALUE)
                                    {
                                        var pattern5 = recurrence as Recurrence.MonthlyPattern; // Intervalpattern
                                        if (pattern5 != null)
                                        {
                                            startIndex = ((days/30)/pattern5.Interval) - bufferZone;
                                        }
                                        else if (startIndex == NO_VALUE)
                                        {
                                            // NOT in the EWS API 2.0 description 
                                            var pattern6 = recurrence as Recurrence.MonthlyRegenerationPattern;
                                            // Intervalpattern
                                            if (pattern6 != null)
                                            {
                                                startIndex = ((days/30)/pattern6.Interval) - bufferZone;
                                            }
                                            else if (startIndex == NO_VALUE)
                                            {
                                                var pattern7 = recurrence as Recurrence.RelativeMonthlyPattern;
                                                // Intervalpattern
                                                if (pattern7 != null)
                                                {
                                                    startIndex = START_INDEX;
                                                }
                                                else if (startIndex == NO_VALUE)
                                                {
                                                    // NOT in the EWS API 2.0 description 
                                                    var pattern = recurrence as Recurrence.YearlyRegenerationPattern;
                                                    // Intervalpattern
                                                    if (pattern != null)
                                                    {
                                                        startIndex = START_INDEX;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    startIndex = startIndex > 1 ? startIndex : START_INDEX;

                    extraInfo = SafeStringFormat.SafeFormat(", period before start date (days): {0}{1}, Pattern interval {2}", days, extraInfo, intervalPattern.Interval);
                }
                else
                {
                    if (startIndex == NO_VALUE)
                    {
                        // This pattern appeares on a specific date each year 
                        // CONSTRAIN: This method will only do when the startDay/endDay period is less that an year.
                        var pattern = recurrence as Recurrence.YearlyPattern; // NO Intervalpattern
                        if (pattern != null)
                        {
                            var theDay = new DateTime(DateTime.Now.Year, (int)pattern.Month, pattern.DayOfMonth);
                            if (theDay >= startDay && theDay <= endDay)
                            {
                                startIndex = DateTime.Now.Year - pattern.StartDate.Year + 1;
                            }
                            else
                            {
                                Logger.LogDebug(LoggingEvents.DebugEvent.EmptyOccurresPeriod(pattern.NumberOfOccurrences.Value, startIndex));
                                startIndex = EMPTY_INDEX;
                            }

                            extraInfo = SafeStringFormat.SafeFormat(" Month: {2} dayOfMonth {1} ", pattern.Month, pattern.DayOfMonth);
                        }
                    }
                    else if (startIndex == NO_VALUE)
                    {
                        // pattern A yearly event that occurre in a specific month
                        // CONSTRAIN: This method will only do when the startDay/endDay period is less that an year.
                        var pattern = recurrence as Recurrence.RelativeYearlyPattern; // NO Intervalpattern
                        if (pattern != null)
                        {
                            var now = DateTime.Now;
                            var firstPossibleDay = new DateTime(now.Year, (int)pattern.Month, 1);
                            int daysInMonth = DateTime.DaysInMonth(now.Year, (int)pattern.Month);
                            var lastPossibleDay = new DateTime(DateTime.Now.Year, (int)pattern.Month, daysInMonth);

                            if (lastPossibleDay >= startDay && firstPossibleDay <= endDay)
                            {
                                startIndex = now.Year - pattern.StartDate.Year + 1;
                            }
                            else
                            {
                                Logger.LogDebug(LoggingEvents.DebugEvent.EmptyOccurresPeriod(pattern.NumberOfOccurrences.Value, startIndex));
                                startIndex = EMPTY_INDEX;
                            }

                            extraInfo = SafeStringFormat.SafeFormat(" Month: {2} DayOfTheWeekIndex {1} ", pattern.Month, pattern.DayOfTheWeekIndex);
                        }
                    }
                }

                Logger.LogDebug(LoggingEvents.DebugEvent.OccurrenceFilterInfo(
                    SafeStringFormat.SafeFormat("{0} Period start/end date: [{1} - {2}]  Occurrence startDay: {3}{4} Start Index {5}", recurrence.GetType().Name, startDay, endDay, recurrence.StartDate, extraInfo, startIndex)));

                if (recurrence.NumberOfOccurrences.HasValue && recurrence.NumberOfOccurrences.Value < startIndex)
                {
                    Logger.LogDebug(LoggingEvents.DebugEvent.EmptyOccurresPeriod(recurrence.NumberOfOccurrences.Value, startIndex));
                    startIndex = EMPTY_INDEX;
                }
            }

            return startIndex;
        }

        /// <summary>
        /// Alternative implementation of retrieving occurring of recurring appointments.
        /// This use the Appointment.BindToOccurrence method call to retrieve each individual items of an recurring appointments.
        /// This method return only the appointments for the recurrings.
        /// </summary>
        /// <param name="recurringMaster"></param>
        /// <param name="startOccurrencesIndex"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        private List<IAppointmentEx> GetOccurrencesFromMasterAppointment(IAppointmentEx recurringMaster, int startOccurrencesIndex, DateTime startDate, DateTime endDate)
        {
            int maxOccurrencesIndex = startOccurrencesIndex + 200;

            var appointments = new List<IAppointmentEx>();

            int beforePeriod = 0;
            int inPeriod = 0;

            for (int i = startOccurrencesIndex; i < maxOccurrencesIndex; i++)
            {
                try
                {
                    IAppointmentEx appointment = _exchangeGateway.GetAppointmentOccurrence(recurringMaster, i);

                    if (startDate > appointment.End)
                    {
                        beforePeriod++;
                        Logger.LogDebug(LoggingEvents.DebugEvent.UnfoldRecurringMasterAppointment(i,
                            SafeStringFormat.SafeFormat(
                                " (Ignore:before [{0};{1}]) Appointment startDay: {2} - {3} (ICalUid {4}, ICalRecurrenceId {5})",
                                startOccurrencesIndex, beforePeriod, appointment.Start, appointment.End, appointment.ICalUid,appointment.ICalRecurrenceId)));
                    }
                    else if (endDate < appointment.Start)
                    {
                        Logger.LogDebug(LoggingEvents.DebugEvent.UnfoldRecurringMasterAppointment(i,
                            SafeStringFormat.SafeFormat(
                                " (Ignore:after  [{0};{1};{2}]) Appointment startDay: {3} - {4} (ICalUid {2}, ICalRecurrenceId {5})",
                                startOccurrencesIndex, beforePeriod, inPeriod, appointment.Start, appointment.End,appointment.ICalUid, appointment.ICalRecurrenceId)));
                        break;
                    }
                    else
                    {
                        inPeriod++;
                        appointments.Add(appointment);
                        Logger.LogDebug(LoggingEvents.DebugEvent.UnfoldRecurringMasterAppointment(i,
                            SafeStringFormat.SafeFormat(
                                " Appointment startDay: {0} - {1} (ICalUid {2}, ICalRecurrenceId {3})",
                                appointment.Start, appointment.End, appointment.ICalUid,appointment.ICalRecurrenceId)));
                    }
                }
                catch (ServiceResponseException ex)
                {
                    if (ex.ErrorCode == ServiceError.ErrorCalendarOccurrenceIndexIsOutOfRecurrenceRange)
                        break;
                    if (ex.ErrorCode == ServiceError.ErrorCalendarOccurrenceIsDeletedFromRecurrence)
                        continue;
                    // It an other error code from the service.
                    throw;
                }
            }

            // Add the delete/removed occurrences by reconstructing from  the DeleteOccurrence collection.
            if (recurringMaster.DeletedOccurrences != null)
            {
                int iCount = 0;
                foreach (var deleteInfo in recurringMaster.DeletedOccurrences)
                {
                    IAppointmentEx appointment = _exchangeGateway.ConvertDeleteReoccurrenceAppointment(recurringMaster,
                        deleteInfo);
                    appointments.Add(appointment);

                    iCount++;
                    Logger.LogDebug(LoggingEvents.DebugEvent.UnfoldRecurringMasterAppointment(iCount,
                        SafeStringFormat.SafeFormat(
                            " Delete Appointment startDay: {0} - {1} (ICalUid {2}, ICalRecurrenceId {3})",
                            appointment.Start, appointment.End, appointment.ICalUid, appointment.ICalRecurrenceId)));
                }
            }

            return appointments;
        }
    }
}