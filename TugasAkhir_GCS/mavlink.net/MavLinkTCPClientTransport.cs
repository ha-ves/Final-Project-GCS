using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MavLinkNet
{
    public class MavLinkTCPClientTransport : MavLinkGenericTransport
    {
        public Socket Socket { get; set; }

        public int HeartBeatUpdateRateMs = 1000;
        public WireProtocolVersion WireProtocolVersion;

        private ConcurrentQueue<byte[]> mReceiveQueue = new ConcurrentQueue<byte[]>();
        private ConcurrentQueue<UasMessage> mSendQueue = new ConcurrentQueue<UasMessage>();
        private AutoResetEvent mReceiveSignal = new AutoResetEvent(true);
        private AutoResetEvent mSendSignal = new AutoResetEvent(true);
        private MavLinkAsyncWalker mMavLink = new MavLinkAsyncWalker();

        private SocketAsyncEventArgs mWifiAsyncEvent = new SocketAsyncEventArgs();
        private bool mIsActive = true;

        public override void Initialize()
        {
            InitializeProtocolVersion(WireProtocolVersion);
            InitializeMavLink();
            InitializeTcpClient();
        }

        public override void Dispose()
        {
            mIsActive = false;

            mReceiveSignal.Set();
            mSendSignal.Set();

            mWifiAsyncEvent.Dispose();
            //Socket.Close();
        }

        private void InitializeMavLink()
        {
            mMavLink.PacketReceived += HandlePacketReceived;
            mMavLink.OtherPacketReceived += HandleOtherPacketReceived;
        }

        private void InitializeTcpClient()
        {
            // Start receive queue worker
            ThreadPool.QueueUserWorkItem(
                new WaitCallback(ProcessReceiveQueue), null);

            // Start send queue worker
            ThreadPool.QueueUserWorkItem(
                new WaitCallback(ProcessSendQueue));

            mWifiAsyncEvent.RemoteEndPoint = Socket.RemoteEndPoint;
            mWifiAsyncEvent.SetBuffer(new byte[1024], 0, 1024);
            mWifiAsyncEvent.Completed += DataReceived;

            Socket.ReceiveAsync(mWifiAsyncEvent);
        }


        // __ Receive _________________________________________________________

        private void DataReceived(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    try
                    {
                        //mMavLink.ProcessReceivedBytes(e.Buffer, 0, e.BytesTransferred);

                        mReceiveQueue.Enqueue(e.Buffer.Take(e.BytesTransferred).ToArray());

                        mReceiveSignal.Set();

                        Socket.ReceiveAsync(mWifiAsyncEvent);
                    }
                    catch (Exception)
                    {
                        mIsActive = false;
                    }

                    break;
            }
        }

        private void ProcessReceiveQueue(object state)
        {
            while (true)
            {
                byte[] buffer;

                if (mReceiveQueue.TryDequeue(out buffer))
                {
                    mMavLink.ProcessReceivedBytes(buffer, 0, buffer.Length);
                }
                else
                {
                    // Empty queue, sleep until signalled
                    mReceiveSignal.WaitOne();

                    if (!mIsActive) break;
                }
            }

            HandleReceptionEnded(this);
        }


        // __ Send ____________________________________________________________


        private void ProcessSendQueue(object state)
        {
            while (true)
            {
                UasMessage msg;

                if (mSendQueue.TryDequeue(out msg))
                {
                    SendMavlinkMessage(msg);
                }
                else
                {
                    // Queue is empty, sleep until signalled
                    mSendSignal.WaitOne();

                    if (!mIsActive) break;
                }
            }
        }

        private void SendMavlinkMessage(UasMessage msg)
        {
            byte[] buffer = mMavLink.SerializeMessage(msg, MavlinkSystemId, MavlinkComponentId, true);

            Socket.Send(buffer, buffer.Length, SocketFlags.None);
        }


        // __ Heartbeat _______________________________________________________


        public void BeginHeartBeatLoop()
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(HeartBeatLoop), null);
        }

        private void HeartBeatLoop(object state)
        {
            while (true)
            {
                foreach (UasMessage m in UavState.GetHeartBeatObjects())
                {
                    SendMessage(m);
                }

                Thread.Sleep(HeartBeatUpdateRateMs);
            }
        }


        // __ API _____________________________________________________________


        public override void SendMessage(UasMessage msg)
        {
            mSendQueue.Enqueue(msg);

            // Signal send thread
            mSendSignal.Set();
        }
    }
}
