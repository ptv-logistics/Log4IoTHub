### Log4IoTHub

Azure IoT Hub appender for log4net... sending messages to the Azure IoT Hub by HttpWebRequest to avoid Service Bus SDK dependencies. The Messages will also be logged/sent
asynchronously for high performance and to avoid blocking the caller thread.

## Use the nuget package 
- **https://www.nuget.org/packages/Log4IoTHub/**
- Add package to your project `Install-Package Log4IoTHub`

## Simple Log4IoTHub Example

This example is also available as a [LoggerTests.cs](https://github.com/ptv-logistics/Log4IoTHub/blob/master/Log4IoTHubTest/LoggerTests.cs):

```csharp
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

```

## Example App Configuration file

This configuration is also available as a [App.config](https://github.com/ptv-logistics/Log4IoTHub/blob/master/Log4IoTHubTest/App.config):


```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <startup>
    <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.5.2" />
  </startup>

  <appSettings>
    <!--
    don't forget to add [assembly: log4net.Config.XmlConfigurator()] to AssemblyInfo.cs
    -->
    <add key="log4net.Config" value="log4net.config"/>
    <add key="log4net.Config.Watch" value="True"/>
  </appSettings>

</configuration>
```

## Example Log4Net Configuration file

Please use the [Device Explorer](https://github.com/Azure/azure-iot-sdks/blob/master/tools/DeviceExplorer/readme.md) to create your own deviceId and deviceKey.
This configuration is also available as a [log4net.config](https://github.com/ptv-logistics/Log4IoTHub/blob/master/Log4IoTHubTest/log4net.config):


```xml
﻿<?xml version="1.0" encoding="utf-8" ?>
<log4net>
  
  <appender name="Log4IoTHubAppender"
               type="Log4IoTHub.Log4IoTHubAppender, Log4IoTHub">

    <!--mandatory id of the Azure IoT Hub device -->
    <deviceId value="YOUR_DEVICE_ID" />
    <!--mandatory primary key of the Azure IoT Hub device -->
    <deviceKey value="YOUR_DEVICE_KEY" />
    <!--optional Azure REST API version -->
    <azureApiVersion value="2015-08-15-preview" />
    <!--mandatory Azure IoT Hub hostname/dns -->
    <iotHubName value="YOUR_IOT_HUB.azure-devices.net" />
    
    <!--optional meta data which should be logged to the IoT Hub-->
    <!--<metaDataJson value="{MetadataVersion:'1.0',Product:'dave',ProductVersion:'2016.1',Lifecycle:'prod',Licence:'full',Customer:'core',Component:'core'}" />-->

    <!-- 
    optional debug setting which should only be used during development or on testsystem.
    Set logMessageToFile=true to inspect your messages (in log4IoTHub_info.log) which will 
    be sent to the Azure IoT Hub.
    -->
    <!--<logMessageToFile value="true"/>-->
    
    <!-- 
    optional name of an logger defined further down with an depending appender e.g. 
    logentries to log internal errors. If the value is empty or the property isn't defined 
    errors will only be logged to log4IoTHub_error.log
    -->
    <!--<errLoggerName value="Log4IoTHubErrors2LogentriesLogger"/>-->
    
    <!-- optional proxy settings
    <webProxyHost value="YOUR_PROXY_HOST_IF_REQUIRED" />
    <webProxyPort value="YOUR_PROXY_PORT_IF_REQUIRED" />
    -->
  </appender>


  <!--
  <appender name="LeAppender" type="log4net.Appender.LogentriesAppender, LogentriesLog4net">
    <immediateFlush value="true" />
    <useSsl value="true" />
    <token value="YOUR_LOGENTRIES_TOKEN" />
    <layout type="log4net.Layout.PatternLayout">
      <param name="ConversionPattern" value="%d{yyyy-MM-dd HH:mm:ss.fff zzz};loglevel=%level%;operation=%m;" />
    </layout>
    <filter type="log4net.Filter.LevelRangeFilter">
      <levelMin value="INFO" />
      <levelMax value="FATAL" />
    </filter>
  </appender>

  <logger name="Log4IoTHubErrors2LogentriesLogger" additivity="false">
    <level value="ALL" />
    <appender-ref ref="LeAppender" />
  </logger>
  -->

  <logger name="Log4IoTHubLogger" additivity="false">
    <appender-ref ref="Log4IoTHubAppender" />
  </logger>
  
</log4net>
```
