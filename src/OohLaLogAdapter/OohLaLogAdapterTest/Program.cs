using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Configure log4net using the .config file
[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace OohLaLogAdapterTest
{
    class Program
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static void Main(string[] args)
        {
            log4net.ThreadContext.Properties["session"] = 21;

            // Log an info level message
            if (log.IsInfoEnabled) log.Info("Application [ConsoleApp] Start");

            // Log a debug message. Test if debug is enabled before
            // attempting to log the message. This is not required but
            // can make running without logging faster.
            if (log.IsDebugEnabled) log.Debug("This is a debug message");

            log.Error("Hey this is an error!");

            // Log an info level message
            if (log.IsInfoEnabled) log.Info("Application [ConsoleApp] End");

            Console.Write("Press Enter to exit...");
            Console.ReadLine();
        }
    }
}
