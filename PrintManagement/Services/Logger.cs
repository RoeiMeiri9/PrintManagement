using System;
using System.IO;
using System.Text;

namespace PrintManagement.Services;

public static class Logger
{
    private static readonly object Gate = new();
    private static readonly string Path_ = System.IO.Path.Combine(
        Windows.Storage.ApplicationData.Current.LocalFolder.Path, "spooler-test.log");

    private record LogLevel(string Value);
    private static readonly LogLevel InfoLvl = new("[INFO] ");
    private static readonly LogLevel WarningLvl = new("[WARNING] ");
    private static readonly LogLevel ErrorLvl = new("[ERROR] ");

    private static void WriteLog(string message, LogLevel level)
    {
        try
        {
            string line = $"{DateTime.Now:HH:mm:ss.fff} {level.Value}{message}{Environment.NewLine}";
            lock (Gate)
            {
                using var fs = new FileStream(Path_, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                byte[] bytes = Encoding.UTF8.GetBytes(line);
                fs.Write(bytes, 0, bytes.Length);
            }
        }
        catch { /* never let logging break the app */ }
    }

    public static void Log(string message)
    {
        WriteLog(message, InfoLvl);
    }

    public static void Error(Exception ex)
    {
        WriteLog(ex.ToString(), ErrorLvl);
    }

    public static void Warning(string message)
    {
        WriteLog(message, WarningLvl);
    }
}
