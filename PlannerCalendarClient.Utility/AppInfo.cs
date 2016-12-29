using System.Diagnostics;
using System.Reflection;

namespace PlannerCalendarClient.Utility
{
    public static class AppInfo
    {
        /// <summary>
        /// Gets the application version number
        /// </summary>
        public static string Version
        {
            get
            {
                var assembly = Assembly.GetEntryAssembly();
                var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
                return fileVersionInfo.FileVersion;
            }
        }

        /// <summary>
        /// Gets the application executable path
        /// </summary>
        public static string ExecutablePath
        {
            get
            {
                var assembly = Assembly.GetEntryAssembly();
                return assembly.Location;
            }
        }

        /// <summary>
        /// Gets the application name
        /// </summary>
        public static string Name
        {
            get
            {
                var assembly = Assembly.GetEntryAssembly();
                return assembly.GetName().Name;
            }
        }
    }
}
