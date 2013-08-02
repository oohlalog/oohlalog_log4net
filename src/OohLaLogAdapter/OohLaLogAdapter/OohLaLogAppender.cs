using System;
using System.IO;
using System.Net;
using System.Collections.Specialized;
using System.Text;

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
        public OohLaLogAppender()
		{
            Layout = new log4net.Layout.PatternLayout(m_defaultLayout);
		}
        private string m_host = "http://www.oohlalog.com/api/logging/save.json";
        private string m_apikey;
        private string m_defaultLayout = "%date [%thread] %-5level %logger - %message%newline";

        #region Properties
        public static long DateTimeToEpochTime(DateTime utc)
        {
            long m_epochReferenceTimeTicks = (new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).Ticks;
            return Convert.ToInt64((new TimeSpan(utc.Ticks - m_epochReferenceTimeTicks)).TotalMilliseconds);
        }
        public string Url { get; set; }
        public string Host
		{
			get { return m_host; }
			set { m_host = value; }
		}

        public string ApiKey
        {
            get { return m_apikey; }
            set { m_apikey = value; }
        }
        #endregion

        #region Methods
        public void SetUrl()
        {
            Url = String.Format("{0}?apiKey={1}", Host, ApiKey);
        }
        #endregion

        #region Override implementation of AppenderSkeleton
        public override void ActivateOptions()
        {
            SetUrl();
        }
		override protected void Append(LoggingEvent loggingEvent) 
		{
			try 
			{
				StringWriter writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
				// Render the event and append the text to the buffer
				RenderLoggingEvent(writer, loggingEvent);
                byte[] response = null;
                using (WebClient client = new MyWebClient())
                {
                    response = client.UploadValues(Url, new NameValueCollection()
                    {
                        { "message", writer.ToString() },
                        { "level", loggingEvent.Level.Name },
                        { "timestamp", DateTimeToEpochTime(loggingEvent.TimeStamp.ToUniversalTime()).ToString()}
                    });
                }
//                var html = Encoding.UTF8.GetString(response);
//                Console.Write(html.ToString());
			} 
			catch(Exception e) 
			{
				ErrorHandler.Error("Error occurred while sending OohLaLog notification.", e);
			}		
		}

		override protected bool RequiresLayout
		{
			get { return true; }
		}
        
		#endregion // Override implementation of AppenderSkeleton

        private class MyWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri uri)
            {
                WebRequest w = base.GetWebRequest(uri);
                w.Timeout = 1000;
                return w;
            }
        }
    }
}