using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airswipe.WinRT.Core.Log
{
    public delegate void LogEvent(EventLevel level, string message);
}
