using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Airswipe.WinRT.Core.MotionTracking
{
    public class Test
    {
        private StreamSocket socket = new StreamSocket();
        private StreamSocketListener _listener = new StreamSocketListener();
        private List<StreamSocket> _connections = new List<StreamSocket>();

        public Test()
        {
             Initialize();
        }

        public async void Initialize()
        {
            _listener.ConnectionReceived += listenerConnectionReceived; ;
            await _listener.BindServiceNameAsync("3011");
            Debug.WriteLine("------ listening on 3011 --");
        }

        void listenerConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            _connections.Add(args.Socket);

            Debug.WriteLine(string.Format("-------- Incoming connection from {0}", args.Socket.Information.RemoteHostName.DisplayName));

            WaitForData(args.Socket);
        }

        async private void WaitForData(StreamSocket socket)
        {
            var dr = new DataReader(socket.InputStream);
            //dr.InputStreamOptions = InputStreamOptions.Partial;
            var stringHeader = await dr.LoadAsync(4);

            if (stringHeader == 0)
            {
                Debug.WriteLine(string.Format("Disconnected (from {0})", socket.Information.RemoteHostName.DisplayName));
                return;
            }

            int strLength = dr.ReadInt32();

            uint numStrBytes = await dr.LoadAsync((uint)strLength);
            string msg = dr.ReadString(numStrBytes);

            Debug.WriteLine(string.Format("Received (from {0}): {1}", socket.Information.RemoteHostName.DisplayName, msg));

            WaitForData(socket);
        }


  
    }

}
