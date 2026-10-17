using System;
using System.IO;
using System.Text;

namespace PrintManagement.Services;

public static class Logger
{
    private static readonly object Gate = new();
    private static readonly string Path_ = System.IO.Path.Combine(
        Windows.Storage.ApplicationData.Current.LocalFolder.Path, "spooler-test.log");

    public static void Log(string message)
    {
        try
        {
            string line = $"{DateTime.Now:HH:mm:ss.fff}  {message}{Environment.NewLine}";
            lock (Gate)
            {
                using var fs = new FileStream(Path_, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                byte[] bytes = Encoding.UTF8.GetBytes(line);
                fs.Write(bytes, 0, bytes.Length);
            }
        }
        catch { /* never let logging break the app */ }
    }
}
