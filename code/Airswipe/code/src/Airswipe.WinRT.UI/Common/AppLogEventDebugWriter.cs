using Airswipe.WinRT.Core;
using Airswipe.WinRT.Core.Log;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airswipe.WinRT.UI.Common
{
    public class AppLogEventDebugWriter : LogEventDebugWriter
    {
        #region Fields

        private static AppLogEventDebugWriter Instance;

        #endregion
        #region Constructors

        private AppLogEventDebugWriter() : base(LogTraceEventSource.Instance, AppSettings.AppLogDebugWriterEventLevel) {
            Debug.WriteLine(typeof(AppLogEventDebugWriter).Name + ": construct");
        }

        public static void Initialize() {
            Instance = new AppLogEventDebugWriter();
        }

        #endregion
    }
}
