using Airswipe.WinRT.Core.Log;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace Airswipe.WinRT.UI.Common
{
    public class AppLogEventTraceListener : LogEventTraceListener
    {
        #region Fields

        private static AppLogEventTraceListener instance;

        public event LogEvent LogEventEmitted;

        public static readonly ObservableCollection<string> LogHistory = new ObservableCollection<string>();

        private const int MAX_HISTORY_LINES_COUNT = Int32.MaxValue; 

        #endregion
        #region Constructors

        private AppLogEventTraceListener(LogTraceEventSource source)
            : base(source)
        {
            Debug.WriteLine(typeof(AppLogEventTraceListener).Name + ": construct");
        }

        public static void InitializeInstance(LogTraceEventSource source)
        {
            instance = new AppLogEventTraceListener(source);
        }

        #endregion
        #region Override methods

        protected override void HandleLogEvent(EventLevel level, string message)
        {
            LogHistory.Add(LogEventFormatter.AsDateTimeTypeMessage(level, message));
            while (LogHistory.Count > MAX_HISTORY_LINES_COUNT)
                LogHistory.RemoveAt(0);

            if (LogEventEmitted != null)
                LogEventEmitted(level, message);
        }

        #endregion
    }
}
