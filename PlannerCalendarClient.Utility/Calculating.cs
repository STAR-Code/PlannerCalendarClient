using System;
using System.Linq;

namespace PlannerCalendarClient.Utility
{
    public static class Calculating
    {
        /// <summary>
        /// This is an extension to TimeSpan. It takes an input of an array of TimeSpan's, finds
        /// the closest (or equal) and calculates TimeSpan between "this" and the found TimeSpan.
        /// Examples: 
        /// - Input: 00:00:00, this: 01:00:00 will return 23:00:00,
        /// - Input: 01:00:00;03:00:00, this: 02:00:00 will return 01:00:00
        /// - Input: 01:00:00;03:00:00, this: 03:00:00 will return 00:00:00
        /// Note it will throw an exception if array is empty or null!
        /// </summary>
        /// <param name="entryPoint"></param>
        /// <param name="times"></param>
        /// <returns></returns>
        public static TimeSpan CalculateIntervalToNextEvent(this TimeSpan entryPoint, TimeSpan[] times)
        {
            TimeSpan next;
            try
            {
                next = times.OrderBy(x => x).First(x => x.Equals(entryPoint));
            }
            catch (Exception)
            {
                try
                {
                    next = times.OrderBy(x => x).First(x => x > entryPoint);
                }
                catch (Exception)
                {
                    next = times.OrderBy(x => x).First().Add(TimeSpan.FromDays(1));
                }
            }
            
            return next - entryPoint;
        }
    }
}
