using System;
using System.Threading;
using System.Collections.Generic;
using System.Timers;
using log4net.Appender;
using log4net.Core;
using log4net.Util;
using log4net.Layout;

namespace OohLaLogAdapter
{
	/// <summary>
	/// Appender that forwards LoggingEvents asynchronously
	/// </summary>
	/// <remarks>
	/// This appender forwards LoggingEvents to OohLaLog appender.
	/// The events are forwarded asynchronously using the ThreadPool.
	/// This allows the calling thread to be released quickly, however it does
	/// not guarantee the ordering of events delivered to the attached appenders.
	/// </remarks>
	public sealed class AsyncOohLaLogAppender : IAppender, IBulkAppender, IOptionHandler, IAppenderAttachable
	{
        private static int DefaultBufferLimit = 150;
        private static double DefaultBufferInterval = 60; // 60 seconds
        private static double DefaultMetricsInterval = 0; //deactivated
        private bool m_bufferenabled = false;
        private bool m_closing = false;
        private AppenderAttachedImpl m_appenderAttachedImpl;
        private List<LoggingEvent> m_events = new List<LoggingEvent>();
        public AsyncOohLaLogAppender()
        {
            Appender = new OohLaLogAppender();
            HostName = Appender.HostName;
            Host = OohLaLogAppender.DefaultHost;
            IsSecure = OohLaLogAppender.DefaultIsSecure;
            Layout = new log4net.Layout.PatternLayout(OohLaLogAppender.DefaultLayout);
            BufferLimit = AsyncOohLaLogAppender.DefaultBufferLimit;
            BufferInterval = AsyncOohLaLogAppender.DefaultBufferInterval;
            MetricsInterval = AsyncOohLaLogAppender.DefaultMetricsInterval;

            this.AddAppender(Appender);
        }

        #region Properties
        //overridable properties
        public string ApiKey { get; set; } //oohlalog api key
        public string Host { get; set; } //oohlalog host
        public string HostName { get; set; }
        public string Name { get; set; }
        public bool IsSecure { get; set; }
        public int BufferLimit { get; set; }
        public double BufferInterval { get; set; } //0 disables buffer
        public double MetricsInterval { get; set; } //0 disables metrics reporting
        public log4net.Layout.PatternLayout Layout { get; set; }

        private OohLaLogAppender Appender { get; set; }
        private System.Timers.Timer BufferTimer { get; set; }
        private System.Timers.Timer MetricsTimer { get; set; }
        #endregion

        #region Methods
        public void ActivateOptions() 
		{
            Appender.HostName = HostName;
            Appender.Layout = Layout;
            Appender.ApiKey = ApiKey;
            Appender.Host = Host;
            Appender.IsSecure = IsSecure;
            if (MetricsInterval > 0)
            {
                Appender.ResourceCounters = new OohLaLogAppender.Counters();
                if (MetricsInterval < 1) MetricsInterval = 1; //1 second minimum
                MetricsTimer = new System.Timers.Timer(MetricsInterval * 1000);
                MetricsTimer.Elapsed += metricstimer_Elapsed;
            }
            if (BufferInterval > 0)
            {
                m_bufferenabled = true;
                if (BufferInterval < 1) BufferInterval = 1; //1 second minimum
                if (BufferLimit < 1) BufferLimit = 1; //1 minimum but very inefficient
                BufferTimer = new System.Timers.Timer(BufferInterval * 1000);
                BufferTimer.Elapsed += buffertimer_Elapsed;
            }
            Appender.ActivateOptions();
            if (MetricsTimer != null) StartMetricsTimer();
            if (BufferTimer != null) StartBufferTimer();
		}

        private void buffertimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            FlushBuffer();
        }
        private void StopBufferTimer()
        {
            if (BufferTimer != null)
                BufferTimer.Stop();
        }
        private void StartBufferTimer()
        {
            if (BufferTimer != null && !m_closing) BufferTimer.Start();
        }
        private void FlushBuffer()
        {
            if (m_events.Count == 0) return;
            AddEvent(null);
        }
        private void StopMetricsTimer()
        {
            if (MetricsTimer != null)
                MetricsTimer.Stop();
        }
        private void StartMetricsTimer()
        {
            if (MetricsTimer != null && !m_closing)
                MetricsTimer.Start();
        }
        private void metricstimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            StopMetricsTimer();
            Appender.sendResourceMetricsPayload();
            StartMetricsTimer();
        }

		public void Close()
		{
			// Remove all the attached appenders
			lock(this)
			{
                m_closing = true;
                StopBufferTimer();
                StopMetricsTimer();
                FlushBuffer();
				if (m_appenderAttachedImpl != null)
				{
					m_appenderAttachedImpl.RemoveAllAppenders();
				}
			}
		}

        public void AddEvent(LoggingEvent logEvent)
        {
            AddEventOrEvents(logEvent, null);
        }
        public void AddEvents(LoggingEvent[] logEvents)
        {
            AddEventOrEvents(null, logEvents);
        }
        public void AddEventOrEvents(LoggingEvent logEvent, LoggingEvent[] logEvents)
        {
            LoggingEvent[] loggingEvents = null;
            bool flushBuffer = false;
            lock (this)
            {
                if (logEvent != null)
                    m_events.Add(logEvent);
                else if (logEvents != null)
                    m_events.AddRange(logEvents);
                else
                    flushBuffer = true;
                if (m_events.Count >= BufferLimit || flushBuffer)
                {
                    StopBufferTimer();
                    if (m_events.Count > 0)
                    {
                        loggingEvents = new LoggingEvent[m_events.Count];
                        m_events.CopyTo(loggingEvents);
                        m_events.Clear();
                    }
                    StartBufferTimer();
                }
            }
            if (loggingEvents != null && !m_closing)
                System.Threading.ThreadPool.QueueUserWorkItem(new WaitCallback(AsyncAppend), loggingEvents);
            else if (loggingEvents != null)
                Appender.DoAppend(loggingEvents);
        }

		public void DoAppend(LoggingEvent loggingEvent)
		{
			loggingEvent.Fix = FixFlags.All;
            if (m_bufferenabled)
                AddEvent(loggingEvent);
            else
                System.Threading.ThreadPool.QueueUserWorkItem(new WaitCallback(AsyncAppend), loggingEvent);
		}

		public void DoAppend(LoggingEvent[] loggingEvents)
		{
			foreach(LoggingEvent loggingEvent in loggingEvents)
			{
				loggingEvent.Fix = FixFlags.All;
			}
            if (m_bufferenabled)
                AddEvents(loggingEvents);
            else
                System.Threading.ThreadPool.QueueUserWorkItem(new WaitCallback(AsyncAppend), loggingEvents);
		}

		private void AsyncAppend(object state)
		{
			if (m_appenderAttachedImpl != null)
			{
				LoggingEvent loggingEvent = state as LoggingEvent;
				if (loggingEvent != null)
				{
					m_appenderAttachedImpl.AppendLoopOnAppenders(loggingEvent);
				}
				else
				{
					LoggingEvent[] loggingEvents = state as LoggingEvent[];
					if (loggingEvents != null)
					{
						m_appenderAttachedImpl.AppendLoopOnAppenders(loggingEvents);
					}
				}
			}
		}
        #endregion

        #region IAppenderAttachable Members

        public void AddAppender(IAppender newAppender) 
		{
			lock(this)
			{
				if (m_appenderAttachedImpl == null) 
				{
					m_appenderAttachedImpl = new log4net.Util.AppenderAttachedImpl();
				}
				m_appenderAttachedImpl.AddAppender(newAppender);
			}
		}

		public AppenderCollection Appenders
		{
			get
			{
				lock(this)
				{
					if (m_appenderAttachedImpl == null)
					{
						return AppenderCollection.EmptyCollection;
					}
					else 
					{
						return m_appenderAttachedImpl.Appenders;
					}
				}
			}
		}

		public IAppender GetAppender(string name) 
		{
			lock(this)
			{
				if (m_appenderAttachedImpl == null || name == null)
				{
					return null;
				}

				return m_appenderAttachedImpl.GetAppender(name);
			}
		}

		public void RemoveAllAppenders() 
		{
			lock(this)
			{
				if (m_appenderAttachedImpl != null) 
				{
					m_appenderAttachedImpl.RemoveAllAppenders();
					m_appenderAttachedImpl = null;
				}
			}
		}

		public IAppender RemoveAppender(IAppender appender) 
		{
			lock(this)
			{
				if (appender != null && m_appenderAttachedImpl != null) 
				{
					return m_appenderAttachedImpl.RemoveAppender(appender);
				}
			}
			return null;
		}

		public IAppender RemoveAppender(string name) 
		{
			lock(this)
			{
				if (name != null && m_appenderAttachedImpl != null)
				{
					return m_appenderAttachedImpl.RemoveAppender(name);
				}
			}
			return null;
		}

		#endregion

	}
}
