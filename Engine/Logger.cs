using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;

namespace Engine
{
    public static class Logger
    {
        private static string DirectoryPath { get; } = Environment.CurrentDirectory + @"\Logs\";
        private static StreamWriter FileStream { get; set; }
        public static event Action<LogMessage> OnLog;

        static Logger()
        {
            FileStream = CreateFilePath();
            OnLog += LogToFile;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
        }

        private static void LogToFile(LogMessage obj)
        {
            string typeLog = obj.Type + ": ";
            string errorMessage = GetDateTimeString(obj.DateTime) + typeLog + obj.Message;
            if (obj.Exception != null)
                errorMessage += " " + obj.Exception;
            FileStream.WriteLine(errorMessage);
        }

        public static void Log(LogType type, string message)
        {
            OnLog?.Invoke(new LogMessage(type, DateTime.Now, message));
        }
        
        public static void Assert([DoesNotReturnIf(false)] bool condition, string message = null)
        {
            if (!condition)
            {
                StackTrace stackTrace = new StackTrace(0, true);
                StringBuilder sb = new StringBuilder("Assertion failed:");
                sb.AppendLine(message);
                sb.AppendLine(stackTrace.ToString());
                
                Log(LogType.Error, sb.ToString());
            }
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            FileStream.WriteLine(GetDateTimeString(DateTime.Now) + "Error: " + ex.Message);
            FileStream.WriteLine("Full Error: " + e.ExceptionObject);
        }

        private static StreamWriter CreateFilePath()
        {
            string path = DirectoryPath + (DateTime.Now.ToString(CultureInfo.InvariantCulture) + ".txt")
                .Replace("/", ".")
                .Replace(" ", "_")
                .Replace(":", "");
            if (!Directory.Exists(DirectoryPath))
                Directory.CreateDirectory(DirectoryPath);
            StreamWriter writer = new StreamWriter(path, true);
            writer.AutoFlush = true;
            return writer;
        }

        private static string GetDateTimeString(DateTime dateTime)
        {
            string dateTimeString = dateTime.ToString(CultureInfo.InvariantCulture)
                .Replace("/", ".") + ": ";
            return dateTimeString;
        }
    }
    public class LogMessage
    {
        public LogType Type { get; }
        public DateTime DateTime { get; }
        public string Message { get; }
        public Exception? Exception { get; }

        public LogMessage(LogType type, DateTime dateTime, string message)
            : this(type, dateTime, message, null) { }

        public LogMessage(LogType type, DateTime dateTime, string message, Exception? exception)
        {
            Type = type;
            DateTime = dateTime;
            Message = message;
            Exception = exception;
        }
    }
    public enum LogType
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }
}