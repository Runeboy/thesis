using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airswipe.WinRT.Core.Log
{
    public class LogEventDebugWriter : LogEventTraceListener
    {
        #region Constructors

        public LogEventDebugWriter(LogTraceEventSource source, EventLevel levelFilter) : base(source, levelFilter)
        {
            Debug.WriteLine("{0}: outputting log events to debug using filter '{1}'", GetType().Name, levelFilter);
        }

        #endregion
        #region Override methods

       protected override void HandleLogEvent(EventLevel level, String message)
        {
            Debug.WriteLine(LogEventFormatter.AsDateTimeTypeMessage(level, message));
        }

        #endregion
    }
}
