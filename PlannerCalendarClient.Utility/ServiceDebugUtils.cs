using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace PlannerCalendarClient.Utility
{
    public static class ServiceDebugUtils
    {
        /// <summary>
        /// Stop the current thread and wait for the user/developer to press ESC key to continue.
        /// </summary>
        /// <param name="msg">The message to show on the console</param>
        public static void WaitForEscKeyToContinue(string msgPromptBefore = "Press ESC key to stop program", string msgPromptAfter = "Process is stopping...")
        {
            ConsoleColor consoleColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(msgPromptBefore);
            Console.Out.Flush();
            Console.ForegroundColor = consoleColor;

            string programName = Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location);
            Console.Title = programName + " (" + msgPromptBefore + ")";
            
            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey(true);
            } while (key.Key != ConsoleKey.Escape);

            consoleColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(msgPromptAfter);
            Console.Out.Flush();
            Console.ForegroundColor = consoleColor;
        }

        /// <summary>
        /// This is a utility method that get the program to wait until the developer
        /// (and debugger) is ready when doing remote debugging by letting the program
        /// wait for developer to press a key. 
        /// This test is only done when running interactive and not debugger is currently 
        /// attached to the debugger AND the command line contain the argument /REMOTEDEBUG
        /// </summary>
        public static string[] WaitForRemoteDebuggerAttach(string[] args)
        {
            const string remoteDebugArgName = "/REMOTEDEBUG";
            bool remoteDebug = args.Contains(remoteDebugArgName, StringComparer.CurrentCultureIgnoreCase);

            if (remoteDebug)
            {
                if (Environment.UserInteractive && !Debugger.IsAttached)
                {
                    var prevForegroundColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Press any key when the debugger has been attached.");
                    Console.Out.Flush();
                    Console.ReadKey(true);
                    Console.ForegroundColor = prevForegroundColor;
                }

                args = (from a in args
                    where (string.Compare(a, remoteDebugArgName, true) != 0)
                    select a).ToArray();
            }

            return args;
        }
    }
}
