OohLaLog log4net Logging Library
------------------------------------

# Using the OohLaLog log4net appender

The OohLaLog adapter is used in conjuction with the log4net library. It can be configured to send logs synchronously or asynchronously to the OohLaLog service.
Configuring log4net is beyond the scope of this document.
 
### Usage

Include the OohLaLogAdapter library into your .NET project. The library DLL (OohLaLogAdapter.dll) can be found in the root of this repository.

To configure for asynchronous logging, modify the log4net configuration in your project config file to include the asynchronous OohLaLog appender:


  <!-- This section contains the log4net configuration settings -->
  <log4net>
    <appender name="AsyncOohLaLogAppender" type="OohLaLogAdapter.AsyncOohLaLogAppender, OohLaLogAdapter">
      <apikey value="<YOUR_API_KEY_HERE>"/>
    </appender>

    <!-- Setup the root category, add the appenders and set the default level -->
    <root>
      <level value="ALL"/>
      <appender-ref ref="AsyncOohLaLogAppender"/>
    </root>
  </log4net>


To configure for synchronous logging, modify the log4net configuration in your project config file to include the non-asynchronous OohLaLog appender:

  <!-- This section contains the log4net configuration settings -->
  <log4net>
    <appender name="OohLaLogAppender" type="OohLaLogAdapter.OohLaLogAppender, OohLaLogAdapter">
      <apikey value="<YOUR_API_KEY_HERE>"/>
    </appender>

    <!-- Setup the root category, add the appenders and set the default level -->
    <root>
      <level value="ALL"/>
      <appender-ref ref="OohLaLogAppender"/>
    </root>
  </log4net>
