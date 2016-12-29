namespace PlannerCalendarClient.Utility
{
    /// <summary>
    /// Common setting for the application.
    /// </summary>
    public static class CommonSettings
    {
        /// <summary>
        /// This datetime format is for formatting date/time in logs and for other data/time tracing information.
        /// </summary>
        public const string TimestampFormat = "yyyy-MM-dd HH:mm:ss";

        /// <summary>
        /// This is the default format for displaying the data/time for calander events.
        /// </summary>
        public const string FullDateTimeFormat = "yyyy-MM-dd HH:mm"; // the same as the "g" format
        /// <summary>
        /// This is the default format for displaying the time for calander events.
        /// </summary>
        public const string TimeFormat = "HH:mm";
    }
}
