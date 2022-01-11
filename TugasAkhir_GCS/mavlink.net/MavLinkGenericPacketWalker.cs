using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace MavLinkNet
{
    public delegate void DataToSendDelegate(object sender, byte[] buffer);

    public delegate void PacketReceivedDelegate(object sender, MavLinkPacketBase packet);

    public abstract class MavLinkGenericPacketWalker
    {
        public static readonly IList<byte> PacketSignalBytes = new ReadOnlyCollection<byte>
            (new List<byte>
                {
                    0xFE,
                    0xFD,
                });

        public static byte PacketSignalByte = byte.MinValue;

        /// <summary>
        /// Event raised everytime a packet is received. This event is synchronous, 
        /// no further packet processing occurs until the event handler returns.
        /// </summary>
        public event PacketReceivedDelegate PacketReceived;

        /// <summary>
        /// Event raised everytime a stream of data fails a CRC check. 
        /// This event is synchronous, no further packet processing occurs until 
        /// the event handler returns.
        /// </summary>
        public event PacketReceivedDelegate PacketDiscarded;

        /// <summary>
        /// Processes a buffer of bytes. When a packet is complete, a PacketReceived 
        /// event is raised.
        /// </summary>
        /// <param name="buffer"></param>
        public abstract void ProcessReceivedBytes(byte[] buffer, int start, int length);


        /// <summary>
        /// Generates the buffer bytes to be sent on the wire for given message.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="systemId"></param>
        /// <param name="componentId"></param>
        /// <param name="includeSignalMark">Whether to include the Packet signal in the buffer or not.</param>
        /// <param name="sequenceNumber">A sequence number for the message, if needed.</param>
        /// <returns></returns>
        public byte[] SerializeMessage(
            UasMessage msg, byte systemId, byte componentId,
            bool includeSignalMark, byte sequenceNumber = 1)
        {
            byte mark = includeSignalMark ? PacketSignalByte : (byte)0;

            switch(PacketSignalByte)
            {
                case 0xFE:
                    return MavLinkPacketV10.GetBytesForMessage(
                         msg, systemId, componentId, sequenceNumber, mark);

                case 0xFD:
                    return MavLinkPacketV20.GetBytesForMessage(
                              msg, systemId, componentId, sequenceNumber, mark);
                default:
                    return new byte[] { byte.MinValue };
            }
        }


        // __ Impl ____________________________________________________________


        protected void NotifyPacketReceived(MavLinkPacketBase packet)
        {
            if (packet == null || PacketReceived == null) return;

            PacketReceived(this, packet);
        }

        protected void NotifyPacketDiscarded(MavLinkPacketBase packet)
        {
            if (packet == null || PacketDiscarded == null) return;

            PacketDiscarded(this, packet);
        }
    }
}
