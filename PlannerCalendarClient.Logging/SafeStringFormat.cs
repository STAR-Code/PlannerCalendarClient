using System;
using System.Linq;
using System.Text;

namespace PlannerCalendarClient.Logging
{
    public static class SafeStringFormat
    {
        public static string SafeFormat(this string formatText, params object[] args)
        {
            string msg;

            if (args != null)
            {
                try
                {
                    msg = string.Format(formatText, args);
                }
                catch (Exception exFormat)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("SAFEFORMAT: Exception thrown when formatting the message: \"" + exFormat.Message +"\"");
                    sb.AppendLine("  The format text is: \"" + (formatText??"(null)") + "\"");

                    if (args.Any())
                    {
                        sb.Append("  The format arguments are:");
                        int argCounter = 0;
                        foreach (object arg in args)
                        {
                            if (argCounter == 0)
                            {
                                sb.Append(";");
                            }

                            if (arg == null)
                            {
                                sb.Append(" (null)");
                            }
                            else
                            {
                                string tmp;

                                try
                                {
                                    if (arg is string)
                                    {
                                        tmp = "\"" + arg.ToString() + "\"";
                                    }
                                    else
                                    {
                                        tmp = arg.ToString();
                                    }
                                }
                                catch (Exception exArg)
                                {
                                    tmp = string.Format("(Exception in ToString: {0})", exArg.Message);
                                }

                                sb.Append(tmp);
                            }

                            argCounter++;
                        }
                        sb.AppendLine();
                    }
                    else
                    {
                        sb.Append("  No arguments!");
                        sb.AppendLine();
                    }

                    sb.AppendLine();
                    sb.AppendLine("Stack Trace:");
                    sb.Append(exFormat.StackTrace);
                    sb.AppendLine();

                    msg = sb.ToString();
                }
            }
            else
            {
                msg = formatText??"(null)";
            }

            return msg;
        }
    }
}
