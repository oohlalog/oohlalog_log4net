OohLaLog log4net Logging Library
------------------------------------

# Using the OohLaLog log4net appender

The OohLaLog adapter is used in conjuction with the log4net library. It can be configured to send logs synchronously or asynchronously to the OohLaLog service.
Configuring log4net is beyond the scope of this document.
 
### Usage

Include the OohLaLogAdapter library into your .NET project. The library DLL (OohLaLogAdapter.dll) can be found in the root of this repository.

To configure for asynchronous logging, modify the log4net configuration in your project config file to include the asynchronous OohLaLog appender:


  <!-- This section contains the log4net configuration settings -->
  &lt;log4net&gt;
    &lt;appender name="AsyncOohLaLogAppender" type="OohLaLogAdapter.AsyncOohLaLogAppender, OohLaLogAdapter"&gt;
      &lt;apikey value="YOUR_API_KEY_HERE"/&gt;
    &lt;/appender&gt;

    &lt;root&gt;
      &lt;level value="ALL"/&gt;
      &lt;appender-ref ref="AsyncOohLaLogAppender"/&gt;
    &lt;/root&gt;
  &lt;/log4net&gt;


To configure for synchronous logging, modify the log4net configuration in your project config file to include the non-asynchronous OohLaLog appender:

  <!-- This section contains the log4net configuration settings -->
  &lt;log4net&gt;
    &lt;appender name="OohLaLogAppender" type="OohLaLogAdapter.OohLaLogAppender, OohLaLogAdapter"&gt;
      &lt;apikey value="YOUR_API_KEY_HERE"/&gt;
    &lt;/appender&gt;

    &lt;root&gt;
      &lt;level value="ALL"/&gt;
      &lt;appender-ref ref="OohLaLogAppender"/&gt;
    &lt;/root&gt;
  &lt;/log4net&gt;
