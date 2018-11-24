using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Airswipe.WinRT.Core.Log
{
    /// <summary> 
    /// This is an advanced usage, where you want to intercept the logging messages and devert them somewhere 
    /// besides ETW. 
    /// </summary> 
    public sealed class StorageFileLogEventListener : EventListener
    {
        /// <summary> 
        /// Storage file to be used to write logs 
        /// </summary> 
        private StorageFile file = null;
        private Type callerType;

        /// <summary> 
        /// Name of the current event listener 
        /// </summary> 
        private string m_Name;

        public StorageFileLogEventListener(Type callerType, LogTraceEventSource logger)
        {
            this.callerType = callerType;

            string name = EventLevel.LogAlways.ToString();

            this.m_Name = name;

            Debug.WriteLine("*** StorageFileEvent Listener for {0} has name {1}", GetHashCode(), name);

            AssignLocalFile();

            EnableEvents(logger, EventLevel.LogAlways);
        }

        private async void AssignLocalFile()
        {
            string filename = m_Name.Replace(" ", "_") + ".log";
            file = await ApplicationData.Current.LocalFolder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);

            Debug.WriteLine("*** filename is: " + filename);
            Debug.WriteLine("*** file path is: " +  ApplicationData.Current.LocalFolder.Path);
        }

        private async void WriteToFile(IEnumerable<string> lines)
        {
            Debug.WriteLine("*** WriteToFile");

            // TODO: 
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {

            Debug.WriteLine("*** OnEventWritten");
            // TODO: \
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {

            // TODO: 
        }
    }
}
