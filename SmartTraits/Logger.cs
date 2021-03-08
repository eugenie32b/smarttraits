using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace SmartTraits
{
    public static class Logger
    {
        public const string DEFAULT_LOG_FILE = "%TEMP%/T4Trait.log";

        public static string LogFile { get; set; } = DEFAULT_LOG_FILE;

        public static void WriteToLog(string s)
        {
            if (LogFile.Contains("%TEMP%"))
                LogFile = LogFile.Replace("%TEMP%", Path.GetTempPath());

            File.AppendAllText(LogFile, "\r\n" + DateTime.Now.ToString("s") + "\r\n" + s);
        }

        public static void WriteToLog(Exception e)
        {
            var sb = new StringBuilder();

            sb.AppendLine(e.Message);
            sb.AppendLine($"Exception type: {e.GetType().Name}");
            sb.AppendLine(e.StackTrace);

            if (e.InnerException != null)
            {
                sb.AppendLine($"Inner exception: {e.InnerException.Message}");
                sb.AppendLine(e.InnerException.StackTrace);
            }

            if (e is ReflectionTypeLoadException loadException)
            {
                foreach (var childEx in loadException.LoaderExceptions)
                {
                    sb.AppendLine($"Load exception: {childEx.Message}");
                }
            }

            WriteToLog(sb.ToString());
        }
    }
}
