using Airswipe.WinRT.Core.Network;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Windows.Networking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Airswipe.WinRT.Core.Log;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.UI;
using Airswipe.WinRT.Core.Data;
using Airswipe.WinRT.Core.MotionTracking;
using Airswipe.WinRT.UI.Common;
using Airswipe.WinRT.Core;
using Windows.UI.Core;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System.Text;
using Airswipe.WinRT.UI.Pages;

namespace Airswipe.WinRT.UI.Controls
{
    public sealed partial class SidebarControl : UserControl
    {
        #region Fields

        private static readonly ILogger log = new TypeLogger<SidebarControl>();

        private ObservableCollection<string> networkAddresses;

        private static IEnumerable<string> machineIpAddresses = NetworkExpert.GetCurrentIpAddresses();

        //        public Color ConnectedColor { get { return Colors.GreenYellow; } }

        private DispatcherTimer ConnectTimeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };

        public event ItemClickEventHandler HideRequest;


        #endregion
        #region Constructors

        public SidebarControl()
        {
            this.InitializeComponent();

            SaveNetworkAddressesWhenTheyChange();

            Loaded += SidebarControl_Loaded;

            UiIndicateConnectivity(false);
        }

        #endregion
        #region Methods

        private void UpdateFrameStatsUi()
        {
            StatsTextBlock.Text = String.Format(
                "Frames received: {0}",
                AppMotionTrackerClient.Instance.FrameCount
                );
        }

        private void SaveNetworkAddressesWhenTheyChange()
        {
            NetworkAddresses.CollectionChanged += (sender, args) =>
            {
                List<string> sorted = NetworkAddresses.ToList();
                sorted.Sort();
                AppSettings.NetworkAddressHistory = sorted;
            };
        }

        public void UiIndicateConnectivity(bool isConnected)
        {
            ConnectButton.IsEnabled = !isConnected;
            DisconnectButton.IsEnabled = isConnected;

            GetLastFrameButton.IsEnabled = isConnected;
            GetDescriptionsButton.IsEnabled = isConnected;

            ContainerGrid.Background = new SolidColorBrush(isConnected ? Colors.Green : Colors.Red);
        }

        private async void ShowMessageDialog(string message)
        {
            await new Windows.UI.Popups.MessageDialog(message).ShowAsync();
        }

        private void Disconnect()
        {
            AppMotionTrackerClient.Instance.Disconnect();
        }

        private async void AddAddressButton_Click(object sender, RoutedEventArgs e)
        {
            ComboBox target = (sender.Equals(AddLocalAddressButton) ? LocalAddressComboxBox : RemoteAddressComboxBox);

            InputMessageDialog dialog = new InputMessageDialog("Provide new address:", target.SelectedItem.ToString());
            if (await dialog.ShowAsync())
            {
                string newAddress = dialog.Value;
                NetworkAddresses.Add(newAddress);

                target.SelectedItem = newAddress;
            }
        }



        #endregion
        #region Event handlers

        private void GetDescriptionsButton_Click(object sender, RoutedEventArgs e)
        {
            AppMotionTrackerClient.Instance.GetDataDescriptions();
        }

        private void ConnectionTypeComboxBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedConnectionTypeString = (sender as ComboBox).SelectedItem.ToString();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs args)
        {
            try
            {
                AppMotionTrackerClient.Instance.Connect(
                    LocalAddressComboxBox.SelectedValue.ToString(),
                    RemoteAddressComboxBox.SelectedValue.ToString()
                    );
            }
            catch (Exception e)
            {
                log.Error("Error during UI connect attempt: " + e.Message);
                UiIndicateConnectivity(false);

                //throw;
                ShowMessageDialog("Failed to connect: " + e.Message);
                Disconnect();
            }

            //            UiIndicateConnectivity(true);
        }

        internal void NotifyVisibilityChange(bool isVisible)
        {
            log.Verbose("Sidebar visibility change: " + isVisible);
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            Disconnect();
        }

        private void Instance_OnDataDescriptionReady(IList<MarkerSet> markerSets, IList<RigidBody> rigidBodies, IList<Skeleton> skeletons)
        {
            DataDescriptionTextBlock.Text = String.Format(
                "MarkerSets:\n{0}\n\nRigid bodies:\n{1}\n\nSkeletons:\n{2}",
                StringExpert.ToJson(markerSets),
                StringExpert.ToJson(rigidBodies),
                StringExpert.ToJson(skeletons)
                );
        }

        private void SidebarControl_Loaded(object sender, RoutedEventArgs e)
        {
            SubscribeToHighFreqEvents(true);

            //AppMotionTrackerClient.Instance.OnFrameBatchReady += Instance_OnFrameBatchReady;
            AppMotionTrackerClient.Instance.OnDataDescriptionReady += Instance_OnDataDescriptionReady;
            AppMotionTrackerClient.Instance.OnConnectSucceeded += (serverDescription) => UiIndicateConnectivity(true);
            AppMotionTrackerClient.Instance.OnDisconnectComplete += (serverDescription) => UiIndicateConnectivity(false);

            AppMotionTrackerBroadcastDetector.Instance.MotionBroadcastDetected += Instance_MotionBroadcastDetected;


            ConnectTimeTimer.Tick += (s, obj) => ConnectTimeTextBlock.Text = "Connect time: " + (AppMotionTrackerClient.Instance.ConnectTime == null ? "(no data)" : (DateTime.Now - AppMotionTrackerClient.Instance.ConnectTime.Value).ToString(@"hh\:mm\:ss"));
            ConnectTimeTimer.Start();

            AddressesTextBlock.Text = StringExpert.CommaSeparate(machineIpAddresses);




            //DatagramSocket socket = new DatagramSocket();
            ////try
            ////{
            //    // Connect to the server (in our case the listener we created in previous step).
            //    await socket.ConnectAsync(new HostName("localhost"), "  1511");
            //   // socket.JoinMulticastGroup(new HostName("239.255.42.99"));

            //    Debug.WriteLine("AUUUUUUUUUUUUUUUUUUUUUUUUUUUW");
            //    var writer = new DataWriter(socket.OutputStream);
            //    writer.WriteString("hellooo");

            //await SendMessage("asdsadsa", 1511);
        }

        private void SubscribeToHighFreqEvents(bool subscribe)
        {
            if (subscribe) {
                log.Verbose("Subscribing to high-freq events");

                AppMotionTrackerClient.Instance.TrackedPointReady += Instance_TrackedPointReady;
                AppTrackerPointProjector.Instance.PlaneNormalTrackingPointProjected += Instance_TrackedPointProjected;
                AppTrackerPointProjector.Instance.DirectionalTrackingPointProjected += Instance_DirectionalTrackingPointProjected;
                AppTrackerPointProjector.Instance.SphericalTrackingPointProjected += Instance_SphericalTrackingPointProjected ;
                AppMotionTrackerClient.Statistics.Updated += Statistics_Updated;
            }
            else
            {
                log.Verbose("Unsubscribing to high-freq events");

                AppMotionTrackerClient.Instance.TrackedPointReady -= Instance_TrackedPointReady;
                AppTrackerPointProjector.Instance.PlaneNormalTrackingPointProjected -= Instance_TrackedPointProjected;
                AppTrackerPointProjector.Instance.DirectionalTrackingPointProjected -= Instance_DirectionalTrackingPointProjected;
                AppTrackerPointProjector.Instance.SphericalTrackingPointProjected -= Instance_SphericalTrackingPointProjected;
                AppMotionTrackerClient.Statistics.Updated -= Statistics_Updated;
            }
        }

        private void Instance_SphericalTrackingPointProjected(WinRT.Core.Data.Dto.ProjectedXYPoint projection)
        {
            SphericalProjectionDistanceTextBlock.Text = Math.Round(projection.ProjectionDistance).ToString();
            SphericalProjectionDistanceSlider.Value = projection.ProjectionDistance;
        }

        private void Instance_DirectionalTrackingPointProjected(WinRT.Core.Data.Dto.ProjectedXYPoint projection)
        {
            DirectionalProjectionDistanceTextBlock.Text = Math.Round(projection.ProjectionDistance).ToString();
            DirectionalProjectionDistanceSlider.Value = projection.ProjectionDistance;

            SphericalDeltaTextBlock.Text = Math.Round(projection.Delta.Length).ToString();
            SphericalDeltaDeltaSlider.Value = projection.Delta.Length;
        }

        private void Statistics_Updated(double framesPerSecond, SpatialPoint expoAvgMovement, double distanceFromLast)
        {
                UpdateFrameStatsUi();


                FrameRateTextBlock.Text = "Frame rate: " + Math.Round(framesPerSecond, 2);
                //DistanceFromLastTextBlock.Text = string.Format("Distance from last: {0}", distanceFromLast);
                //ExpAvgMovementTextBlock.Text = string.Format("Movement(exp avg): (\n\tx:{0} \n\ty:{1} \n\tz:{2}\n\t)", expoAvgMovement.X, expoAvgMovement.Y, expoAvgMovement.Z);    
        }

        private void Instance_TrackedPointProjected(WinRT.Core.Data.Dto.ProjectedXYPoint projection)
        {
            ProjectionDistanceTextBlock.Text = Math.Round(projection.ProjectionDistance).ToString(); //Math.Round(projection.ProjectionDistance, 2);
            ProjectionDistanceSlider.Value = projection.ProjectionDistance; //Math.Round(projection.ProjectionDistance, 2);

            //DistanceFromLastTextBlock.Text = string.Format("Projection delta length: {0}", Math.Round(delta.Length, 2));

            //if (delta.Length < 100 && delta.Length > ProjectionDeltaSlider.Maximum)
            //{
            //    ProjectionDeltaSlider.Maximum = Math.Ceiling(delta.Length);
            //    //DistanceFromLastTextBlock.Text = string.Format("Projection delta length (0..{0}):", DistanceFromLastSlider.Maximum);
            //    //DeltaLengthMaxTextBlock.Text = DistanceFromLastSlider.Maximum.ToString();

            //    ProjectionDeltaTextBlock.Text = string.Format("Projection delta length (0..{0})", ProjectionDeltaSlider.Maximum);
            //}

            ProjectionDeltaTextBlock.Text = projection.Delta.Length.ToString(); // Math.Round(delta.Length, 2).ToString();
            ProjectionDeltaSlider.Value = projection.Delta.Length;
        }

        private void Instance_TrackedPointReady(OffscreenPoint[] t)
        {
            var point = t[0];


            //if (point.Delta.Length < 0.5 && point.Delta.Length > DistanceFromLastSlider.Maximum)
            //{ 
            //    DistanceFromLastSlider.Maximum = Math.Round(point.Delta.Length,2);
            //    //DistanceFromLastTextBlock.Text = string.Format("Projection delta length (0..{0}):", DistanceFromLastSlider.Maximum);
            //    //DeltaLengthMaxTextBlock.Text = DistanceFromLastSlider.Maximum.ToString();

            //    DistanceFromLastTextBlock.Text = string.Format("Tracking delta length (0..{0})", DistanceFromLastSlider.Maximum);
            //}
            SpatialDeltaTextBlock.Text = Math.Round(point.Delta.Length, 2).ToString();
            SpatialDeltaSlider.Value = point.Delta.Length * 1000;

            //const int decimals = 4;
            //TrackedPointTextBlock.Text = string.Format("Tracked point: (x:{0} \ty:{1} \tz:{2})", 
            //    Math.Round(point.X, decimals),
            //    Math.Round(point.Y, decimals),
            //    Math.Round(point.Z, decimals)
            //    );

            //TrackedPointTextBlock.Text = string.Format("Tracked point: {0}", (DateTime.Now - point.CaptureTime).TotalMinutes);



            //UpdateFrameStatsUi();
        }



        private async Task SendMessage(string message, int port)
        {
            var socket = new DatagramSocket();

            socket.MessageReceived += SocketOnMessageReceived;

            using (var stream = await socket.GetOutputStreamAsync(new HostName("255.255.255.255"), port.ToString()))
            {
                using (var writer = new DataWriter(stream))
                {
                    var data = Encoding.UTF8.GetBytes(message);
                    writer.WriteBytes(data);
                    writer.StoreAsync();
                }
            }
        }

        private async void SocketOnMessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            var result = args.GetDataStream();
            var resultStream = result.AsStreamForRead(1024);

            using (var reader = new StreamReader(resultStream))
            {
                var text = await reader.ReadToEndAsync();
                Debug.WriteLine("#####################");
                //Deployment.Current.Dispatcher.BeginInvoke(() =>
                //{
                //    // Do what you need to with the resulting text
                //    // Doesn't have to be a messagebox
                //    MessageBox.Show(text);
                //});
            }
        }

        private async void Instance_MotionBroadcastDetected(HostName remoteAddress)
        {
            // Process on UI thread or we'll get punished immediately by the OS
            await Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                {
                    string address = remoteAddress.ToString();
                    if (!NetworkAddresses.Contains(address))
                        NetworkAddresses.Add(address);

                    MotionBroadcasterAddressTextBlock.Text = "Motion broadcaster detected at: " + address;
                }
                );
        }

        //private void Instance_OnFrameBatchReady(List<FrameOfMocapData> data, SimplifiedMotionTrackerClient client)
        //{
        //    UpdateFrameStatsUi();
        //}

        private void AppFrameFileSimulation_CheckChange(object sender, RoutedEventArgs e)
        {
            AppFrameFileReader.Instance.IsEnabled = (sender as CheckBox).IsChecked.Value;
        }
        private void HideButton_Click(object sender, RoutedEventArgs e)
        {
            if (HideRequest != null)
                HideRequest(this, null);

            SubscribeToHighFreqEvents(false);
        }

        private static IEnumerable<string> GetDefaultNetworkAddresses()
        {
            return machineIpAddresses.Union(new string[] { "127.0.0.1" });
        }

        private void GetLastFrameButton_Click(object sender, RoutedEventArgs args)
        {
            ShowMessageDialog("Not implemented");
            //AppMotionTrackerClient.Instance.GetLastFrameOfData(true);
            //TODO: does not communicate with server
        }

        private void PingButton_Click(object sender, RoutedEventArgs e)
        {
            ShowMessageDialog("Not implemented");
        }

        private void AppCsvFrameFileSimulation_CheckChange(object sender, RoutedEventArgs e)
        {
            bool isChecked = (sender as CheckBox).IsChecked.Value;

            if (isChecked)
            {
                string filepath = CsvFileTextBox.Text;

                AppCsvFileFrameSimulator.Instance.End += (s, a) => Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    () => (sender as CheckBox).IsChecked = false
                    );
                AppCsvFileFrameSimulator.Instance.Start(filepath);
            }
            else
                AppCsvFileFrameSimulator.Instance.Stop();
        }

        //private void AppOffscreenBoundariesFrameFileSimulation_CheckChange(object sender, RoutedEventArgs e)
        //{
        //    bool isChecked = (sender as CheckBox).IsChecked.Value;
        //    //AppCsvFileFrameSimulator.Instance.IsContinuous = isChecked;
        //}

        private void ParseCsvContinuouslyCheckBox_CheckChange(object sender, RoutedEventArgs e)
        {
            bool isChecked = (sender as CheckBox).IsChecked.Value;
            AppCsvFileFrameSimulator.Instance.IsContinuous = isChecked;
        }

        #endregion
        #region Properties

        public string SelectedLocalNetworkAddress
        {
            get { return AppSettings.SelectedLocalNetworkAddress; }
            set { AppSettings.SelectedLocalNetworkAddress = value; }
        }

        public string SelectedServerNetworkAddress
        {
            get { return AppSettings.SelectedServerNetworkAddress; }
            set { AppSettings.SelectedServerNetworkAddress = value; }
        }

        public ObservableCollection<string> NetworkAddresses
        {
            get
            {
                if (networkAddresses == null)
                    networkAddresses = new ObservableCollection<string>(
                        AppSettings.NetworkAddressHistory ?? GetDefaultNetworkAddresses()
                        );

                return networkAddresses;
            }
        }

        public IEnumerable<string> ConnectionTypes
        {
            get { return Enum.GetValues(typeof(ConnectionType)).Cast<Enum>().Select(v => v.ToString()); }
        }

        public ConnectionType SelectedConnectionType
        {
            get { return (ConnectionType)Enum.Parse(typeof(ConnectionType), SelectedConnectionTypeString); }
        }

        public string SelectedConnectionTypeString
        {
            get
            {
                return AppSettings.SelectedConnectionTypeString ?? ConnectionType.Multicast.ToString();
            }
            set
            {
                if (AppSettings.SelectedConnectionTypeString == value)
                    return;

                AppSettings.SelectedConnectionTypeString = value;

                Disconnect();
                AppMotionTrackerClient.InstanceConnectionType = SelectedConnectionType;

                ConnectButton.Content = String.Format("Connect using v{0} client", AppMotionTrackerClient.Instance.VersionString);
            }
        }

        public void NotifiedContainerVisibilityChanged(bool isVisible)
        {
            // TODO: attach/detach event handlers
        }

        #endregion

        private void ProjectionCursorVisibility_CheckChange(object sender, RoutedEventArgs e)
        {
            MasterPage.RequestCursorVisibility((sender as CheckBox).IsChecked.Value);
        }

        private void ScrollViewer_ManipulationCompleted(object sender, Windows.UI.Xaml.Input.ManipulationCompletedRoutedEventArgs e)
        {
            //(sender as ScrollViewer).ScrollToVerticalOffset(e.Position.Y);
            //e.Position
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MasterPage.RequestCursorToggle();
        }
    }
}
