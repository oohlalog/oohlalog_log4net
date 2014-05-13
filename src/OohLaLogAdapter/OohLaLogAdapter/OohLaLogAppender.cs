using System;
using System.IO;
using System.Net;
using System.Collections.Specialized;
using System.Text;
using System.Diagnostics;
using System.Web;

using log4net.Layout;
using log4net.Core;
using log4net.Appender;

namespace OohLaLogAdapter
{
	/// <summary>
	/// OohLaLog appender that sends individual messages
	/// </summary>
	/// <remarks>
	/// This OohLaLog sends each LoggingEvent received as a
	/// separate url post message to OohLaLog service.
	/// </remarks>
	public class OohLaLogAppender : AppenderSkeleton
	{
        public static string DefaultHost = "api.oohlalog.com";
        public static string DefaultLayout = "%date [%thread] %-5level %logger - %message%newline";
        public static bool DefaultIsSecure = false;
        private static string DefaultUrlPath = "api/logging/save.json";
        private static string DefaultMetricsUrlPath = "api/timeSeries/save.json";
        public OohLaLogAppender()
		{
            HostName = System.Environment.MachineName;
            Host = OohLaLogAppender.DefaultHost;
            IsSecure = OohLaLogAppender.DefaultIsSecure;
            Layout = new log4net.Layout.PatternLayout(OohLaLogAppender.DefaultLayout);
		}

        #region Properties
        //overridable properties
        public string HostName { get; set; }
        public string Host { get; set; }
        public string ApiKey { get; set; }
        public bool IsSecure {get; set;}

        private string Url { get; set; }
        private string MetricsUrl { get; set; }
        #endregion

        #region Methods
        private void SetUrl()
        {
            Url = String.Format("{0}://{1}/{2}?apiKey={3}",(IsSecure ? "https" : "http"), Host
                , DefaultUrlPath,ApiKey);
            MetricsUrl = String.Format("{0}://{1}/{2}?apiKey={3}", (IsSecure ? "https" : "http"), Host, DefaultMetricsUrlPath, ApiKey);
        }
        private void sendPayload(string payload)
        {
            try
            {
                using (WebClient client = new MyWebClient())
                {
                    client.Headers.Add("Content-Type", "application/json");
                    string response = client.UploadString(Url, String.Format("{{\"logs\": [{1}]}}", HostName,payload));
                }
            }
            catch (Exception e)
            {
                ErrorHandler.Error("Error occurred while sending OohLaLog notification.", e);
            }
        }
        private string buildJsonString(LoggingEvent loggingEvent)
        {
            if (loggingEvent == null)
                return "";
            StringWriter writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
            RenderLoggingEvent(writer, loggingEvent);
            return String.Format("{{\"hostName\": \"{3}\",\"message\":\"{0}\",\"level\":\"{1}\",\"timestamp\":{2}}}",
                        HttpUtility.JavaScriptStringEncode(writer.ToString()),
                        loggingEvent.Level.Name,
                        DateTimeToEpochTime(loggingEvent.TimeStamp.ToUniversalTime()).ToString(),
                        HostName);
        }
        private static long DateTimeToEpochTime(DateTime utc)
        {
            long m_epochReferenceTimeTicks = (new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).Ticks;
            return Convert.ToInt64((new TimeSpan(utc.Ticks - m_epochReferenceTimeTicks)).TotalMilliseconds);
        }
        #endregion

        #region Override implementation of AppenderSkeleton
        public override void ActivateOptions()
        {
            SetUrl();
        }
        override protected void Append(LoggingEvent[] loggingEvents)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var l in loggingEvents)
            {
                sb.Append((sb.Length == 0 ? "" : ",") + buildJsonString(l));
            }
            sendPayload(sb.ToString());
        }
		override protected void Append(LoggingEvent loggingEvent) 
		{
            sendPayload(buildJsonString(loggingEvent));
		}
		override protected bool RequiresLayout
		{
			get { return true; }
		}

		#endregion // Override implementation of AppenderSkeleton

        #region Resource Metrics
        public Counters ResourceCounters { get; set; }
        public void sendResourceMetricsPayload()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(String.Format("\"cpu.cpuUsage\":{0:0.0}", ResourceCounters.TotalCpu));
                sb.Append(String.Format(",\"cpu.process.cpuUsage\":{0:0.00}", ResourceCounters.ProcessCpu));
                sb.Append(String.Format(",\"memory.physical.freeBytes\":{0:0.00}", ResourceCounters.TotalAvailableRam));
                sb.Append(String.Format(",\"memory.physical.totalBytes\":{0:0.00}", ResourceCounters.TotalRam));
                sb.Append(String.Format(",\"memory.process.usedMemory\":{0:0.00}", ResourceCounters.ProcessRam));
                string payload = String.Format("{{\"host\": \"{0}\", \"metrics\":{{{1}}}}}", HostName, sb.ToString());
                using (WebClient client = new MyWebClient())
                {
                    client.Headers.Add("Content-Type", "application/json");
                    string response = client.UploadString(MetricsUrl, payload);
                }
            }
            catch (Exception e)
            {
                ErrorHandler.Error("Error occurred while sending OohLaLog metric notification.", e);
            }
        }
        #endregion

        #region supporting classes
        private class MyWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri uri)
            {
                WebRequest w = base.GetWebRequest(uri);
                w.Timeout = 1000;
                return w;
            }
        }
        public class Counters
        {
            private static int Mbyte2ByteMultiplier = 1024 * 1024;
            public Counters()
            {
                Counters.Initialize(this);
            }
            public string ProcessName { get; set; }
            public float TotalRam { get; set; }
            public float TotalAvailableRam { get { return TotalRamCounter.NextValue() * Mbyte2ByteMultiplier; } }
            public float TotalCpu { get { return TotalCpuCounter.NextValue(); } }
            public float ProcessCpu { get { return ProcessCpuCounter.NextValue(); } }
            public float ProcessRam { get { return ProcessRamCounter.NextValue(); } }

            private PerformanceCounter TotalCpuCounter;
            private PerformanceCounter TotalRamCounter;
            private PerformanceCounter ProcessCpuCounter;
            private PerformanceCounter ProcessRamCounter;
            private static void Initialize(Counters c)
            {
                c.ProcessName = Process.GetCurrentProcess().ProcessName;
                c.TotalRam = Convert.ToInt64(new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory);
                c.TotalCpuCounter = new PerformanceCounter()
                {
                    CategoryName = "Processor",
                    CounterName = "% Processor Time",
                    InstanceName = "_Total"
                };
                c.TotalRamCounter = new PerformanceCounter()
                {
                    CategoryName = "Memory",
                    CounterName = "Available MBytes"
                };
                c.ProcessCpuCounter = new PerformanceCounter()
                {
                    CategoryName = "Process",
                    CounterName = "% User Time",
                    InstanceName = c.ProcessName
                };
                c.ProcessRamCounter = new PerformanceCounter()
                {
                    CategoryName = "Process",
                    CounterName = "Working Set",
                    InstanceName = c.ProcessName
                };
            }
        }
        #endregion
    }
}