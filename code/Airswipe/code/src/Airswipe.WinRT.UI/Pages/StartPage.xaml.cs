using Airswipe.WinRT.UI.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Airswipe.WinRT.Core.Log;
using Windows.UI.Xaml.Shapes;
using Windows.UI;
using Windows.UI.Xaml.Media.Animation;
using Airswipe.WinRT.UI;
using Airswipe.WinRT.UI.Pages;
using Airswipe.WinRT.UI.DataModel;

// The Items Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234233

namespace Airswipe.WinRT.UI.Pages
{
    /// <summary>
    /// A page that displays a collection of item previews.  In the Split App this page
    /// is used to display and select one of the available groups.
    /// </summary>
    public sealed partial class StartPage : Page
    {
        private ILogger log = new TypeLogger<StartPage>();

        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();


        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and 
        /// process lifetime management
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        public StartPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;


            log.Verbose("start page intialized");

            //MetroEventSource.Log.Info("Current Window is activating");

            //// First time execution, initialize the logger 
            //EventListener verboseListener = new StorageFileLogEventListener("MyListenerVerbose");
            //EventListener informationListener = new StorageFileLogEventListener("MyListenerInformation");

            //verboseListener.EnableEvents(LogEventSource.Log, EventLevel.Verbose);
            //informationListener.EnableEvents(LogEventSource.Log, EventLevel.Informational);

        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private async void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            // TODO: Create an appropriate data model for your problem domain to replace the sample data
            var sampleDataGroups = await SampleDataSource.GetGroupsAsync();
            this.DefaultViewModel["Items"] = sampleDataGroups;
        }

        /// <summary>
        /// Invoked when an item is clicked.
        /// </summary>
        /// <param name="sender">The GridView (or ListView when the application is snapped)
        /// displaying the item clicked.</param>
        /// <param name="e">Event data that describes the item clicked.</param>
        void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Navigate to the appropriate destination page, configuring the new page
            // by passing required information as a navigation parameter

           // Console.Clear();


            var groupId = ((SampleDataGroup)e.ClickedItem).UniqueId;
            
            Debug.WriteLine("Clicked group id: " + groupId);


           this.Frame.Navigate(GetNexFrameType(groupId), groupId);
        }

        private Type GetNexFrameType(String groupId)
        {
            switch (groupId)
            {
                case "Calibrate": return typeof(CalibratePage);
                case "LogViewer": return typeof(LogViewerPage);
                case "Settings": return typeof(SettingsPage);
                case "DistanceMeasure": return typeof(DistanceMeasurePage);
                case "Test": return typeof(TestPage);
                case "Kinect": return typeof(KinectPage);
                case "Data": return typeof(DataPage);
            }
            throw new NotImplementedException();
        }

        #region NavigationHelper registration

        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// 
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="GridCS.Common.NavigationHelper.LoadState"/>
        /// and <see cref="GridCS.Common.NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion
    }
}
