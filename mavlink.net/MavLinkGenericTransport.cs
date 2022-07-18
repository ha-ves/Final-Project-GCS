using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MavLinkNet
{
    public abstract class MavLinkGenericTransport : IDisposable
    {
        public byte MavlinkSystemId = 200;
        public byte MavlinkComponentId = 1;
        public byte PacketSignalByte = byte.MinValue;
        public MavLinkState UavState = new MavLinkState();

        public event DataToSendDelegate OnPacketToSend;

        public event PacketReceivedDelegate OnPacketReceived, OnPacketDiscarded;

        public event EventHandler OnReceptionEnded;

        public abstract void Initialize();
        public abstract void Dispose();
        public abstract void SendMessage(UasMessage msg);
        //public abstract void SendRawPacket(MavLinkPacket packet);

        public void InitializeProtocolVersion(WireProtocolVersion wireProtocolVersion)
        {
            switch (wireProtocolVersion)
            {
                case WireProtocolVersion.v10:
                    MavLinkAsyncWalker.PacketSignalByte = 0xFE;
                    break;
                case WireProtocolVersion.v20:
                    MavLinkAsyncWalker.PacketSignalByte = 0xFD;
                    break;
                default:
                    break;
            }
        }

        public virtual void DataReceived(object sender, byte[] data)
        {

        }

        // __ MavLink events __________________________________________________

        protected void HandleDataToSend(object sender, byte[] buffer)
        {
            if (OnPacketToSend != null) OnPacketToSend(sender, buffer);
        }

        protected void HandlePacketReceived(object sender, MavLinkPacketBase e)
        {
            if (OnPacketReceived != null) OnPacketReceived(sender, e);
        }

        protected void HandleReceptionEnded(object sender)
        {
            if (OnReceptionEnded != null) OnReceptionEnded(sender, EventArgs.Empty);
        }

        protected void HandlePacketDiscarded(object sender, MavLinkPacketBase packet)
        {
            if (OnPacketDiscarded != null) OnPacketDiscarded(sender, packet);
        }
    }
}
