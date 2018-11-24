using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airswipe.WinRT.Core.Log
{
    public class LogTraceEventSource : EventSource
    {
        #region Fields

        internal static LogTraceEventSource instance;

        #endregion
        #region Methods

        public static LogTraceEventSource Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new LogTraceEventSource();
                    Debug.WriteLine("{0}: instance intialized", instance.GetType().Name);
                }

                return instance;
            }
        }

        [Event(1, Level = EventLevel.Verbose)]
        public void Verbose(string message)
        {
            this.WriteEvent(1, message);
        }

        [Event(2, Level = EventLevel.Informational)]
        public void Informational(string message)
        {
            this.WriteEvent(2, message);
        }

        [Event(3, Level = EventLevel.Warning)]
        public void Warning(string message)
        {
            this.WriteEvent(3, message);
        }

        [Event(4, Level = EventLevel.Error)]
        public void Error(string message)
        {
            this.WriteEvent(4, message);
        }

        [Event(5, Level = EventLevel.Critical)]
        public void Critical(string message)
        {
            this.WriteEvent(5, message);
        }

        [Event(6, Level = EventLevel.LogAlways)]
        public void LogAlways(string message)
        {
            this.WriteEvent(6, message);
        }

        #endregion
    }
}
