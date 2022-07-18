using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;

namespace MavLinkNet
{
    public class MavLinkLogFileTransport : MavLinkGenericTransport
    {
        private string mLogFileName;

        public MavLinkLogFileTransport(string logFileName)
        {
            mLogFileName = logFileName;
        }

        public override void Initialize()
        {
            Parse();
        }

        public override void Dispose()
        {
            
        }

        public override void SendMessage(UasMessage msg)
        {
            // No messages are sent on this transport (only read from the logfile)
        }


        // __ Impl ____________________________________________________________


        private void Parse()
        {
            try
            {
                using (FileStream s = new FileStream(mLogFileName, FileMode.Open))
                {
                    using (BinaryReader reader = new BinaryReader(s))
                    {
                        while (true)
                        {
                            MavLinkPacketBase packet = null;
                            switch (SyncStream(reader))
                            {
                                case 0xFE:
                                    PacketSignalByte = 0xFE;
                                    packet = MavLinkPacketV10.Deserialize(reader, 0);
                                    break;

                                case 0xFD:
                                    PacketSignalByte = 0xFD;
                                    packet = MavLinkPacketV20.Deserialize(reader, 0);
                                    break;

                                default:
                                    break;
                            }

                            if (packet.IsValid)
                            {
                                HandlePacketReceived(this, packet);
                            }
                        }
                    }
                }
            }
            catch (EndOfStreamException)
            { 
                
            }

            HandleReceptionEnded(this);
        }

        private byte SyncStream(BinaryReader s)
        {
            byte delimiter = byte.MinValue;
            do
            {
                // Skip bytes until a packet start is found
                delimiter = s.ReadByte();
            } while (!MavLinkGenericPacketWalker.PacketSignalBytes.Contains(delimiter));
            
            return delimiter;
        }
    }
}
