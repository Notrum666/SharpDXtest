using System;
using System.IO;
using System.Threading.Tasks;

namespace SharpDXtest.Assets.Components;
public static class Logger
{
    public static string NameLogger { get; set; }
    private static string DirectoryPath { get; set; } = Environment.CurrentDirectory + "/Logs";

    //TODO: нужно обсудить что именно еще добавить при использовании разных типов.
    public static async Task AddMessage(LogType type, string message)  
    {
        string typeLog = "";
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
        using StreamWriter writer = new StreamWriter(DirectoryPath, true);
        await writer.WriteLineAsync(typeLog + message);
    }

    public static async Task CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        using StreamWriter writer = new StreamWriter(DirectoryPath, true);
        await writer.WriteLineAsync("Error: " + e.ExceptionObject);
    }
}

public enum LogType
{
    Info = 0,
    Warning = 1,
    Error = 2
}