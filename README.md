OohLaLog log4net Logging Library
------------------------------------

# Using the OohLaLog log4net appender

The OohLaLog adapter is used in conjuction with the log4net library. It can be configured to send logs synchronously or asynchronously to the OohLaLog service.
Configuring log4net is beyond the scope of this document.
 
### Usage

Include the OohLaLogAdapter library into your .NET project. The library DLL (OohLaLogAdapter.dll) can be found in the root of this repository.

To configure for asynchronous logging, modify the log4net configuration in your project config file to include the asynchronous OohLaLog appender:


```html
  <!-- This section contains the log4net configuration settings -->
  <log4net>
    <appender name="AsyncOohLaLogAppender" type="OohLaLogAdapter.AsyncOohLaLogAppender, OohLaLogAdapter">
    	<!--required settings-->
	<apikey value="YOUR_API_KEY_HERE"/>
    	<!--optional settings:
      		issecure: true or false; default is false
      		hostname: default is machine name as returned from System.Environment.MachineName
      		bufferlimit: number of logs to buffer before posting to OLL (lower numbers impact app performance); default is 150
      		bufferinterval: age in seconds of logs in buffer before automatic posting to OLL (lower numbers impact app performance); default is 60 seconds
		layout: format of log message. See log4net documentation for details
      	<bufferlimit value="150"/>
      	<bufferinterval value="60"/>
      	<issecure value="false"/>
      	<hostname value="test-pc"/>
	<layout type="log4net.Layout.PatternLayout" value="%date %-5level %logger - %message%newline"/>
    	-->
    </appender>

    <root>
      <level value="ALL"/>
      <appender-ref ref="AsyncOohLaLogAppender"/>
    </root>
  </log4net>
```
