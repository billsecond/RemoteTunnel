using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcpTunnel.Utils
{
    public static class Logger
    {
        static public TextWriter? LogWatcher
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
