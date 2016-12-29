using System;

namespace PlannerCalendarClient.Utility
{
    /// <summary>
    /// Utility methods for formatting exception to string for using in logs and shown in the UI.
    /// </summary>
    public static class ExceptionUtils
    {
        /// <summary>
        /// Formatting exception with innner exception to string.
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static string ExceptionToStringMessage(Exception ex)
        {
            const int maxNestedInnerExceptions = 10;

            var msg = string.Format("Excpetion Message: {0} (Exception type: {1}){2}", ex.Message, ex.GetType().Name, Environment.NewLine);

            int counter = 0;
            Exception innerEx = ex.InnerException;
            while (innerEx != null)
            {
                counter++;
                msg += string.Format("\n  Inner exception {0}: {1} (Exception type: {2}){3}", counter, innerEx.Message, innerEx.GetType().Name, Environment.NewLine);
                innerEx = innerEx.InnerException;

                if (counter >= maxNestedInnerExceptions)
                {
                    msg += string.Format("\nMaximum of {0} inner exception is shown. The rest are ignored.", maxNestedInnerExceptions);
                    break;
                }
            }

            return msg;
        }
    }
}
