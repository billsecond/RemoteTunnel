using System.IO;


namespace Utils
{
    public static class Logger
    {
        static public TextWriter LogWatcher
        {
            get;
            set;
        }
        static public void WriteLineLog(string message)
        {
            if (Logger.LogWatcher != null) Logger.LogWatcher.WriteLine(message);
        }
    }
}
