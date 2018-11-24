using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airswipe.WinRT.Core.Log
{
    public class LogEventTraceListener : EventListener
    {
        #region Fields


        #endregion
        #region Constructors

        public LogEventTraceListener(LogTraceEventSource source) : this(source, EventLevel.LogAlways) { }

        public LogEventTraceListener(LogTraceEventSource source, EventLevel levelFilter)
        {
            EnableEvents(source, levelFilter);
        }

        #endregion
        #region Methods

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            EventLevel level = eventData.Level;
            string message = eventData.Payload[0].ToString();
            HandleLogEvent(level, message);
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        { }

        protected virtual void HandleLogEvent(EventLevel level, String message)
        { }
        
        #endregion
    }
}
