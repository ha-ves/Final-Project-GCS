/*
The MIT License (MIT)

Copyright (c) 2019, Mikael Ferland

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
using System.IO;
using System.Text;

namespace MavLinkNet
{
    public class MavLinkPacketV20 : MavLinkPacketBase
    {
        public const int PacketOverheadNumBytes = 10;

        public byte IncompatibilityFlags;
        public byte CompatibilityFlags;

        // __ Deserialization _________________________________________________

        /*
         * Byte order:
         * 
         * 0  Packet start sign	
         * 1	 Payload length	 0 - 255
         * 2     Incompatibility Flags
         * 3     Compatibility Flags
         * 4	 Packet sequence	 0 - 255
         * 5	 System ID	 1 - 255
         * 6	 Component ID	 0 - 255
         * 7to9	 Message ID	 0 - 16777215
         * 10 to (n+10)	 Data	 (0 - 255) bytes
         * (n+11) to (n+12)	 Checksum (high byte, low byte) for v0.9, lowbyte, highbyte for 1.0 and 2.0
         * (n+12) to (n+26)  Signature (optional) Not supported for now
         */
        public static MavLinkPacketV20 Deserialize(BinaryReader s, byte payloadLength)
        {
            MavLinkPacketV20 result = new MavLinkPacketV20()
            {
                PayLoadLength = (payloadLength == 0) ? s.ReadByte() : payloadLength,
                IncompatibilityFlags = s.ReadByte(),
                CompatibilityFlags = s.ReadByte(),
                PacketSequenceNumber = s.ReadByte(),
                SystemId = s.ReadByte(),
                ComponentId = s.ReadByte(),
                MessageId = BitConverter.ToUInt32(PadRightByteArray(s.ReadBytes(3)), 0),
            };
            
            result.WireProtocolVersion = WireProtocolVersion.v20;

            // Read the payload instead of deserializing so we can validate CRC.
            result.Payload = s.ReadBytes(result.PayLoadLength);
            result.Checksum1 = s.ReadByte();
            result.Checksum2 = s.ReadByte();

            if (result.IsValidCrc())
            {
                result.DeserializeMessage();
            }

            return result;
        }

        public override int GetPacketSize()
        {
            return PacketOverheadNumBytes + PayLoadLength;
        }

        private bool IsValidCrc()
        {
            UInt16 crc = GetPacketCrc(this);

            return ( ((byte)(crc & 0xFF) == Checksum1) &&
                     ((byte)(crc >> 8) == Checksum2) );
        }

        private void DeserializeMessage()
        {
            UasMessage result = UasSummary.CreateFromId((byte)MessageId);
                        
            if (result == null) return;  // Unknown type

            // Zero-bytes are truncated at the end of the packet
            // We have to fill the payload in that case otherwise the binary ready will throw exception
            FillPayloadIfNeeded(result);

            using (MemoryStream ms = new MemoryStream(Payload))
            {
                using (BinaryReader br = GetBinaryReader(ms))
                {
                    result.DeserializeBody(br);
                }
            }

            Message = result;
            IsValid = true;
        }

        private static byte[] PadLeftByteArray(byte[] sourceArray)
        {
            var newArray = new byte[3 + 1];
            sourceArray.CopyTo(newArray, 1);
            newArray[0] = 0;
            return newArray;
        }

        private static byte[] PadRightByteArray(byte[] sourceArray)
        {
            var newArray = new byte[3 + 1];
            sourceArray.CopyTo(newArray, 0);
            newArray[3] = 0;
            return newArray;
        }

        private void FillPayloadIfNeeded(UasMessage message)
        {
            if(Payload.Length < message.PacketSize)
            {
                var newArray = new byte[message.PacketSize];
                Payload.CopyTo(newArray, 0);
                Payload = newArray;
            }
        }

        // __ Serialization ___________________________________________________


        public static MavLinkPacketV20 GetPacketForMessage(
            UasMessage msg, byte systemId, byte componentId, byte sequenceNumber)
        {
            MavLinkPacketV20 result = new MavLinkPacketV20()
            {
                SystemId = systemId,
                ComponentId = componentId,
                PacketSequenceNumber = sequenceNumber,
                MessageId = msg.MessageId,
                Message = msg
            };

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    msg.SerializeBody(bw);
                }

                result.Payload = ms.ToArray();
                result.PayLoadLength = (byte)result.Payload.Length;
                result.UpdateCrc();
            }

            return result;
        }

        public static byte[] GetBytesForMessage(
            UasMessage msg, byte systemId, byte componentId, byte sequenceNumber, byte signalMark)
        {
            MavLinkPacketV20 p = MavLinkPacketV20.GetPacketForMessage(
                                 msg, systemId, componentId, sequenceNumber);

            int bufferSize = p.GetPacketSize();

            if (signalMark != 0) bufferSize++;

            using (MemoryStream s = new MemoryStream())
            {
                using (BinaryWriter w = new BinaryWriter(s))
                {
                    if (signalMark != 0) w.Write(signalMark);
                    p.Serialize(w);
                }

                return s.ToArray();
            }
        }
                
        public override void Serialize(BinaryWriter w)
        {
            w.Write(PayLoadLength);
            w.Write(IncompatibilityFlags);
            w.Write(CompatibilityFlags);
            w.Write(PacketSequenceNumber);
            w.Write(SystemId);
            w.Write(ComponentId);
            w.Write(MessageId);
            w.Write(Payload);
            w.Write(Checksum1);
            w.Write(Checksum2);
        }

        private void UpdateCrc()
        {
            UInt16 crc = GetPacketCrc(this);
            Checksum1 = (byte)(crc & 0xFF);
            Checksum2 = (byte)(crc >> 8);
        }

        public static UInt16 GetPacketCrc(MavLinkPacketV20 p)
        {
            UInt16 crc = X25CrcSeed;

            crc = X25CrcAccumulate(p.PayLoadLength, crc);
            crc = X25CrcAccumulate(p.IncompatibilityFlags, crc);
            crc = X25CrcAccumulate(p.CompatibilityFlags, crc);
            crc = X25CrcAccumulate(p.PacketSequenceNumber, crc);
            crc = X25CrcAccumulate(p.SystemId, crc);
            crc = X25CrcAccumulate(p.ComponentId, crc);
            crc = X25CrcAccumulate((byte)(p.MessageId & 0x0000FF), crc);
            crc = X25CrcAccumulate((byte)((p.MessageId & 0x00FF00) >> 8), crc);
            crc = X25CrcAccumulate((byte)((p.MessageId & 0xFF0000) >> 16), crc);

            for (int i = 0; i < p.Payload.Length; ++i)
            {
                crc = X25CrcAccumulate(p.Payload[i], crc);
            }

            crc = X25CrcAccumulate(UasSummary.GetCrcExtraForId((byte)p.MessageId), crc);

            return crc;
        }



        //// __ CRC _____________________________________________________________


        //// CRC code adapted from Mavlink C# generator (https://github.com/mavlink/mavlink)

        //const UInt16 X25CrcSeed = 0xffff;

        //public static UInt16 X25CrcAccumulate(byte b, UInt16 crc)
        //{
        //    unchecked
        //    {
        //        byte ch = (byte)(b ^ (byte)(crc & 0x00ff));
        //        ch = (byte)(ch ^ (ch << 4));
        //        return (UInt16)((crc >> 8) ^ (ch << 8) ^ (ch << 3) ^ (ch >> 4));
        //    }
        //}
    }
}
