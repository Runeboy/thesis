using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airswipe.WinRT.Core.Log
{
    public class TypeLogger<T> : ILogger
    {
        #region Fields

        public LogTraceEventSource logger;

        #endregion
        #region Constructors

        public TypeLogger(LogTraceEventSource logger)
        {
            this.logger = logger;

            //Debug.WriteLine("{0} with sender type '{1}': initialized", GetType().Name, typeof(T).Name);
        }

        public TypeLogger() : this(LogTraceEventSource.Instance) { }

        #endregion
        #region Methods

        private string FormatMessage(string message, EventLevel eventLevel, params object[] args)
        {
            return typeof(T).Name + ": " + (args.Length == 0 ? message : String.Format(message, args));
        }

        public void Critical(string message, params object[] args)
        {
            logger.Critical(FormatMessage(message, EventLevel.Critical, args));
        }

        public void Info(string message, params object[] args)
        {
            logger.Informational(FormatMessage(message, EventLevel.Informational, args));
        }

        public void Warn(string message, params object[] args)
        {
            logger.Warning(FormatMessage(message, EventLevel.Warning, args));
        }

        public void Error(string message, params object[] args)
        {
            logger.Error(FormatMessage(message, EventLevel.Error, args));
        }

        public void All(string message, params object[] args)
        {
            logger.LogAlways(FormatMessage(message, EventLevel.LogAlways, args));
        }

        public void Verbose(string message, params object[] args)
        {
            logger.Verbose(FormatMessage(message, EventLevel.Verbose, args));
        }

        #endregion
    }
}
