using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using SharpDX.Text;
using Exception = ABI.System.Exception;

namespace SharpDXtest.Assets.Components;
public static class Logger 
{
    private static string DirectoryPath { get; } = Environment.CurrentDirectory + @"\Logs\";
    private static StreamWriter FileStream { get; set; }
    public static event Action<LogMessage> OnLog;

    static Logger()
    {
        FileStream = CreateFilePath();
        OnLog += LogAdd;
    }

    private static void LogAdd(LogMessage obj)  
    {
        string typeLog = "";
        switch (obj.Type)
        {
            case LogType.Info:
            {
                typeLog = "Info: ";
                break;
            }
            case LogType.Warning:
            {
                typeLog = "Warning: ";
                break;
            }
            case LogType.Error:
            {
                typeLog = "Error: ";
                break;
            }
        }
        var errorMessage = GetDataString(obj.DateTime) + typeLog + obj.Message;
        if (obj.Exception != null)
        {
            errorMessage += " " + obj.Exception;
        }
        FileStream.WriteLine(errorMessage);
    }

    public static void Log(LogType type, string message)
    {
         OnLog?.Invoke(new LogMessage(type, DateTime.Now, message));
    }

    public static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        FileStream.WriteLine(GetDataString(DateTime.Now) +  "Error: " + e.ExceptionObject);
    }
    
    private static StreamWriter CreateFilePath()
    {
        string path = DirectoryPath + (DateTime.Now.ToString(CultureInfo.InvariantCulture) + ".txt")
            .Replace("/", ".")
            .Replace(" ", "_")
            .Replace(":", ""); 
        StreamWriter writer = new StreamWriter(path, true);
        writer.AutoFlush = true;
        return writer;
    }
    private static string GetDataString(DateTime dateTime)
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

    public LogMessage(LogType type, DateTime dateTime, string errorMessage)
    {
        Type = type;
        DateTime = dateTime;
        Message = errorMessage;
    }
    public LogMessage(LogType type, DateTime dateTime, Exception? exception)
    {
        Type = type;
        DateTime = dateTime;
        Exception = exception;
    }
}
public enum LogType
{
    Info = 0,
    Warning = 1,
    Error = 2
}