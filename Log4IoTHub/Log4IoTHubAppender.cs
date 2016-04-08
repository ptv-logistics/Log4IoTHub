//--------------------------------------------------------------
// Copyright (c) 2016 PTV Group
// 
// For license details, please refer to the file LICENSE, which 
// should have been provided with this distribution.
//--------------------------------------------------------------

using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using System;
using System.Reflection;
using System.Threading;

namespace Log4IoTHub
{
    public class Log4IoTHubAppender : AppenderSkeleton
    {
        private static ILog log;
        private static ILog extraLog;

        private LoggingEventSerializer serializer;

        private IoTHubClient iotHubClient;

        public string DeviceId { get; set; }
        public string DeviceKey { get; set; }
        public string AzureApiVersion { get; set; }
        public string IotHubName { get; set; }
        public string WebProxyHost { get; set; }
        public int? WebProxyPort { get; set; }

        public string MetaDataJson { get; set; }

        private static bool logMessageToFile = false;
        public bool LogMessageToFile { get; set; }
 
        public string ErrLoggerName { get; set; }


        static Log4IoTHubAppender()
        {
        }


        public override void ActivateOptions()
        {

            try
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Log4IoTHub.InternalLog4net.config"))
                {
                    XmlConfigurator.Configure(stream);
                }

                log = LogManager.GetLogger("Log4IoTHubInternalLogger");

                if (!string.IsNullOrWhiteSpace(ErrLoggerName))
                {
                    extraLog = LogManager.GetLogger(ErrLoggerName);
                }
                

                logMessageToFile = LogMessageToFile;

                if (string.IsNullOrWhiteSpace(DeviceId))
                {
                    throw new Exception($"the Log4IoTHubAppender property deviceId [{DeviceId}] shouldn't be empty");
                }

                if (string.IsNullOrWhiteSpace(DeviceKey))
                {
                    throw new Exception($"the Log4IoTHubAppender property deviceKey [{DeviceKey}] shouldn't be empty");
                }

                if (string.IsNullOrWhiteSpace(IotHubName))
                {
                    throw new Exception($"the Log4IoTHubAppender property iotHubName [{IotHubName}] shouldn't be empty");
                }


                iotHubClient = IoTHubClient.Instance(DeviceId, DeviceKey, AzureApiVersion, IotHubName, WebProxyHost, WebProxyPort);
                serializer = new LoggingEventSerializer
                {
                    MetaDataJson = MetaDataJson
                };
            }
            catch (Exception ex)
            {
                Error($"Unable to activate Log4IoTHubAppender: {ex.Message}");
            }
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            try
            {
                if (iotHubClient != null)
                {
                    var content = serializer.SerializeLoggingEvents(new[] { loggingEvent });
                    Info(content);
                    //http://www.ben-morris.com/using-asynchronous-log4net-appenders-for-high-performance-logging/
                    ThreadPool.QueueUserWorkItem(task => iotHubClient.IoTHubRequestAsync(content));
                }
           }
            catch (Exception ex)
            {
                Error($"Unable to send message to EventHub: {ex}");
            }
        }


        public static void Error(string logMessage, bool async = true)
        {
            if (log != null)
            {
                if (async)
                {
                    //http://www.ben-morris.com/using-asynchronous-log4net-appenders-for-high-performance-logging/
                    ThreadPool.QueueUserWorkItem(task => log.Error(logMessage));
                    if (extraLog != null)
                    {
                        ThreadPool.QueueUserWorkItem(task => extraLog.Error(logMessage));
                    }
                }
                else
                {
                    log.Error(logMessage);
                    if (extraLog != null)
                    {
                        extraLog.Error(logMessage);
                    }

                }
            }
        }

        public static void Info(string logMessage)
        {
            if (logMessageToFile && log != null)
            {
                //http://www.ben-morris.com/using-asynchronous-log4net-appenders-for-high-performance-logging/
                ThreadPool.QueueUserWorkItem(task => log.Info(logMessage));
            }
        }

    }
}
