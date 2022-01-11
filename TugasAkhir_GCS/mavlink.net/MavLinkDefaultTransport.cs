/*
The MIT License (MIT)

Copyright (c) 2014, Håkon K. Olafsen

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
using System;
using System.Threading;
using System.Collections.Concurrent;

namespace MavLinkNet
{
    public class MavLinkDefaultTransport : MavLinkGenericTransport
    {
        public int HeartBeatUpdateRateMs = 1000;
        public WireProtocolVersion WireProtocolVersion;

        private ConcurrentQueue<byte[]> mReceiveQueue = new ConcurrentQueue<byte[]>();
        private ConcurrentQueue<UasMessage> mSendQueue = new ConcurrentQueue<UasMessage>();
        
        private AutoResetEvent mReceiveSignal = new AutoResetEvent(true);
        private AutoResetEvent mSendSignal = new AutoResetEvent(true);

        private MavLinkAsyncWalker mMavLink = new MavLinkAsyncWalker();

        private bool mIsActive = true;

        public override void Initialize()
        {
            InitializeProtocolVersion(WireProtocolVersion);
            InitializeMavLink();

            // Start receive queue worker
            ThreadPool.QueueUserWorkItem(
                new WaitCallback(ProcessReceiveQueue), null);

            // Start send queue worker
            ThreadPool.QueueUserWorkItem(
                new WaitCallback(ProcessSendQueue));

        }

        public override void Dispose()
        {
            mIsActive = false;

            mReceiveSignal.Set();
            mSendSignal.Set();

            mMavLink.Dispose();
        }

        private void InitializeMavLink()
        {
            mMavLink.PacketReceived += HandlePacketReceived;
        }

        // __ Receive _________________________________________________________
        
        
        public override void DataReceived(object sender, byte[] data)
        {
            mReceiveQueue.Enqueue(data);

            // Signal processReceive thread
            mReceiveSignal.Set();
        }

        private void ProcessReceiveQueue(object state)
        {
            Thread.CurrentThread.Name = "ProcessReceiveQueue";

            while (true)
            {
                byte[] buffer;

                mReceiveSignal.WaitOne();

                if (!mIsActive) break;

                if (mReceiveQueue.TryDequeue(out buffer))
                {
                    mMavLink.ProcessReceivedBytes(buffer, 0, buffer.Length);
                }
            }

            HandleReceptionEnded(this);
        }


        // __ Send ____________________________________________________________


        private void ProcessSendQueue(object state)
        {
            Thread.CurrentThread.Name = "ProcessSendQueue";

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
            var serialized = mMavLink.SerializeMessage(msg, MavlinkSystemId, MavlinkComponentId, true);
            HandleDataToSend(this, serialized);
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
