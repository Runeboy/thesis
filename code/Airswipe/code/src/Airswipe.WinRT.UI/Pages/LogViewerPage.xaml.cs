using Airswipe.WinRT.Core.Log;
using Airswipe.WinRT.UI.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Airswipe.WinRT.UI.Pages
{
    public sealed partial class LogViewerPage : BasicPage
    {
        #region Fields


        #endregion
        #region Constructors

        public LogViewerPage()
        {
            this.InitializeComponent();
        }

        #endregion 
        #region Properties

        public ObservableCollection<string> LogLinesSource
        {
            get { return AppLogEventTraceListener.LogHistory; }
        }

        #endregion
    }
}
