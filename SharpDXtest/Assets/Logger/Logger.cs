using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using SharpDX.Text;

namespace SharpDXtest.Assets.Components;
public static class Logger 
{
    public static string NameLogger { get; set; }
    private static string DirectoryPath { get; set; } = Environment.CurrentDirectory + @"\Logs\";
    private static StreamWriter FileStream { get; set; }

    static Logger()
    {
        FileStream = CreateFilePath();
    }

    public static async Task<string> AddMessage(LogType type, string message)  
    {
        string typeLog = NameLogger + ": ";
        switch (type)
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
        string errorMessage = typeLog + message;
        await FileStream.WriteLineAsync(errorMessage);
        return errorMessage;
    }

    public static async Task<string> CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        string errorMessage = "Error: " + e.ExceptionObject;
        await FileStream.WriteLineAsync(errorMessage);
        return errorMessage;
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
}

public enum LogType
{
    Info = 0,
    Warning = 1,
    Error = 2
}