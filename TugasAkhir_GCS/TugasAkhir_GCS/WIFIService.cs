using MavLinkNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TugasAkhir_GCS.Interfaces;

namespace TugasAkhir_GCS
{
    public delegate void WIFIDataReceived(object sender, byte[] data);

    public class WIFIService : IReceiverService
    {
        public event NewDataReceived DataReceived;

        SocketAsyncEventArgs asyncEvent;

        Socket NetSocket;
        byte[] RxBuf;

        public async Task<bool> ConnectTo(string hostname, ushort port)
        {
            var ip = Dns.GetHostAddresses(hostname)[0];
            var endpoint = new IPEndPoint(ip, port);

            //use UDP
            NetSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            await Task.WhenAny(Task.Delay(5000), Task.Run(() =>
            {
                try
                {
                    NetSocket.Connect(endpoint);
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"TCP Connect Exception :\r\n{e}");
                }
            }));

            if (NetSocket.Connected)
            {
                RxBuf = new byte[4096];
                NetSocket.Send(Encoding.ASCII.GetBytes("hello"));
                NetSocket.BeginReceive(RxBuf, 0, RxBuf.Length, SocketFlags.None, WIFIReceived, null);

                if (await (App.Current as App).InitializeTransport())
                    return true;
            }

            await Disconnect();

            Debug.WriteLine("Used TCP. Client not responding.");

            return false;
        }

        public Task<bool> Disconnect()
        {
            if(asyncEvent != null)
                asyncEvent.Dispose();

            if(NetSocket != null)
            {
                if (NetSocket.Connected)
                    NetSocket.Close();
                NetSocket.Dispose();
            }

            RxBuf = null;
            DataReceived = null;

            return Task.FromResult(true);
        }

        private void WIFIReceived(IAsyncResult ar)
        {
            if (NetSocket == null)
                return;

            int bytes = 0;
            try
            {
                bytes = NetSocket.EndReceive(ar);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception)
            {
                Debugger.Break();
                return;
            }
            
            NetSocket.BeginReceive(RxBuf, 0, RxBuf.Length, SocketFlags.None, WIFIReceived, null);

            //Debug.WriteLine($"New WIFI Data -> {bytes}");
            //int count = 0;
            //for (int i = 0; i < e.BytesTransferred; i++)
            //{
            //    Debug.Write($" {e.Buffer[i]:X2} ");
            //    if (++count > 15)
            //    {
            //        Debug.WriteLine("");
            //        count = 0;
            //    }
            //}
            //Debug.WriteLine("");

            if (DataReceived != null) DataReceived(this, RxBuf.Take(bytes).ToArray());
        }

        public void SendData(object sender, byte[] buffer) => NetSocket.Send(buffer);

        #region not used implementations

        public Task<string[]> RefreshSerialPorts()
        {
            return Task.FromResult(new string[0]);
        }

        public Task<bool> ConnectTo(string portName, string baudrate)
        {
            return Task.FromResult(false);
        }

        #endregion
    }
}
