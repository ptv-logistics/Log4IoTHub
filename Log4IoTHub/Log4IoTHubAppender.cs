//--------------------------------------------------------------
// Copyright (c) 2016 PTV Group
// 
// For license details, please refer to the file LICENSE, which 
// should have been provided with this distribution.
//--------------------------------------------------------------

using log4net;
using log4net.Appender;
using log4net.Core;
using System;
using System.Threading;

namespace Log4IoTHub
{
    public class Log4IoTHubAppender : AppenderSkeleton
    {
        private static ILog _log;

        private LoggingEventSerializer _serializer;

        private IoTHubClient _iotHubClient;

        public string DeviceId { get; set; }
        public string DeviceKey { get; set; }
        public string AzureApiVersion { get; set; }
        public string IotHubName { get; set; }
        public string WebProxyHost { get; set; }
        public int? WebProxyPort { get; set; }

        public string MetaDataJson { get; set; }

        private static bool _logError = false;
        public bool LogError { get; set; }

        private static bool _logMessageToFile = false;
        public bool LogMessageToFile { get; set; }

        public string LoggerName { get; set; }



        static Log4IoTHubAppender()
        {
        }


        public override void ActivateOptions()
        {

            try
            {
                _log = LogManager.GetLogger(LoggerName);
                _logError = LogError;
                _logMessageToFile = LogMessageToFile;

                _iotHubClient = IoTHubClient.Instance(DeviceId, DeviceKey, AzureApiVersion, IotHubName, WebProxyHost, WebProxyPort);
                _serializer = new LoggingEventSerializer
                {
                    MetaDataJson = MetaDataJson
                };
            }
            catch (Exception ex)
            {
                Error($"Unable to connect to EventHub: {ex}");
            }
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            try
            {
                if (_iotHubClient != null)
                {
                    var content = _serializer.SerializeLoggingEvents(new[] { loggingEvent });
                    Info(content);
                    //http://www.ben-morris.com/using-asynchronous-log4net-appenders-for-high-performance-logging/
                    ThreadPool.QueueUserWorkItem(task => _iotHubClient.IoTHubRequestAsync(content));
                }
           }
            catch (Exception ex)
            {
                Error($"Unable to send message to EventHub: {ex}");
            }
        }


        public static void Error(string logMessage, bool async = true)
        {
            if (_logError && _log != null)
            {
                if (async)
                {
                    //http://www.ben-morris.com/using-asynchronous-log4net-appenders-for-high-performance-logging/
                    ThreadPool.QueueUserWorkItem(task => _log.Error(logMessage));
                }
                else
                {
                    _log.Error(logMessage);
                }
            }
        }

        public static void Info(string logMessage)
        {
            if (_logMessageToFile && _log != null)
            {
                //http://www.ben-morris.com/using-asynchronous-log4net-appenders-for-high-performance-logging/
                ThreadPool.QueueUserWorkItem(task => _log.Info(logMessage));
            }
        }

    }
}
