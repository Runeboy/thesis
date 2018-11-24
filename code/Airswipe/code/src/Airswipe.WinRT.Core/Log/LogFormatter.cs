using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airswipe.WinRT.Core.Log
{
    public static class LogEventFormatter
    {
        #region Methods

        public static string AsDateTimeTypeMessage(EventLevel level, string message)
        {
            return String.Format(
                "[{0:yyyy-MM-dd HH\\:mm\\:ss} {1}] {2}", 
                DateTime.Now, 
                level.ToString().ToUpper(), 
                message
                );
        }

        #endregion
    }
}
