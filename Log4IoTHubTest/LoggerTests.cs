using log4net;
using System;

namespace Log4IoTHubTest
{
    class LoggerTests
    {

        private static ILog iotHubLogger = LogManager.GetLogger("Log4IoTHubLogger");

        static void Main(string[] args)
        {
 
            for (int i = 0; i < 2000; i++)
            {
                iotHubLogger.Info(new { id = $"mob-{i}", message = $"hallo-{i}" });

            }


            System.Threading.Thread.Sleep(new TimeSpan(0, 10, 0));
        }
    }
}
