using MavLinkNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using TugasAkhir_GCS.Interfaces;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TugasAkhir_GCS
{
    public partial class App : Application
    {
        public IReceiverService ReceiverService;
        
        public MavLinkDefaultTransport MavLinkTransport;
        public ManualResetEventSlim MavLinkCmdAck = new ManualResetEventSlim(false);

        /* Transport variables */
        Aes enc, dec;

        // dummy mavlink 2.0 attitude msg
        public List<byte> DummyBuf = new List<byte>()
        { 0xfd, 0x10, 0x00, 0x00, 0x3a, 0x00, 0xc8, 0x1e, 0x00, 0x00, 0xbd, 0x12, 0x01, 0x00, 0x83, 0xa1,
            0xe9, 0xbb, 0xb4, 0x2d, 0x4b, 0xbc, 0x37, 0xd0, 0xaa, 0x3f, 0x0e, 0xa6 };

        /* stopwatch benchmark */
        public DateTime packettime;

        public IFileHandler attsavefile, gpssavefile, syssavefile, hrtsavefile;

        public App()
        {
            Thread.CurrentThread.Name = "MainApp Thread";

            InitializeComponent();

            MainPage = new MainPage();

            (MainPage as MainPage).HideLoadingOverlay();

            DeviceDisplay.KeepScreenOn = true;

#if DATA_FETCH
            var time = DateTime.Now.ToString("yyyy-MM-dd_ss-mm-HH");

            attsavefile = DependencyService.Get<IFileHandler>(DependencyFetchTarget.NewInstance);
            attsavefile.Initialize($"Attitude_ProcessTime_{time}.csv");
            gpssavefile = DependencyService.Get<IFileHandler>(DependencyFetchTarget.NewInstance);
            gpssavefile.Initialize($"GPS_ProcessTime_{time}.csv");
            syssavefile = DependencyService.Get<IFileHandler>(DependencyFetchTarget.NewInstance);
            syssavefile.Initialize($"SysStat_ProcessTime_{time}.csv");
            hrtsavefile = DependencyService.Get<IFileHandler>(DependencyFetchTarget.NewInstance);
            hrtsavefile.Initialize($"HrtBt_ProcessTime_{time}.csv");
#endif
        }

        internal void FinishDataFetch()
        {
            attsavefile.Finish();
            gpssavefile.Finish();
            syssavefile.Finish();
            hrtsavefile.Finish();
        }

        #region Transport

        public Mutex consolemutex = new Mutex(false);

        public async Task<bool> InitializeTransport()
        {
#if DATA_FETCH
            attsavefile.WriteLine("ATTITUDE (IMU SENSOR) UPDATE UI TIME (ms)");
            //attsavefile.WriteLine("Decrypt Time (ms),Parse time (ms),UI update time (ms)");
            gpssavefile.WriteLine("GPS SENSOR UPDATE UI TIME (ms)");
            //gpssavefile.WriteLine("Decrypt Time (ms),Parse time (ms),UI update time (ms)");
            syssavefile.WriteLine("SYS STAT UPDATE UI TIME (ms)");
            //syssavefile.WriteLine("Decrypt Time (ms),Parse time (ms),UI update time (ms)");
            hrtsavefile.WriteLine("HEARTBEAT UPDATE UI TIME (ms)");
            //hrtsavefile.WriteLine("Decrypt Time (ms),Parse time (ms),UI update time (ms)");
#endif
            if (MavLinkTransport != null)
                MavLinkTransport.Dispose();

            MavLinkTransport = new MavLinkDefaultTransport();

            InitializeAES();

            //if (consolemutex.WaitOne(0))
            //{
            //    Debug.Write($"Key -> [ ");
            //    for (int i = 0; i < dec.Key.Length; i++)
            //    {
            //        Debug.Write($" {dec.Key[i]:X2} ");
            //    }
            //    Debug.WriteLine("]");

            //    Debugger.Break();

            //    consolemutex.ReleaseMutex();
            //}

            ReceiverService.DataReceived += AESDecryptProcess;
            MavLinkTransport.OnPacketToSend += AESEncryptProcess;

            return await InitMavLink();
        }

        private void AESEncryptProcess(object sender, byte[] buffer)
        {
            try
            {
                var cipher = EncryptAndGetCipherBytes(buffer);

                ReceiverService.SendData(sender, cipher);
            }
            catch (CryptographicException cryptExc)
            {
                Debug.WriteLine("AESEncryptProcess Exception.\r\n");
                return;
            }
        }




        public bool ErrorMavlink = false;

        private void AESDecryptProcess(object sender, byte[] data, DateTime receiveTime)
        {
            packettime = receiveTime;

            try
            {
                var plain = (App.Current as App).DecryptAndGetPlainBytes(data);

                if (ErrorMavlink)
                {
                    byte[] rand = { 0, 0 };
                    RandomNumberGenerator.Create().GetNonZeroBytes(rand);
                    if (rand[0] > plain.Length) rand[0] = (byte)(plain.Length - 1);
                    plain[rand[0]] = rand[1];
                    ErrorMavlink = false;
                }

                MavLinkTransport.DataReceived(sender, plain);

                //Debug.WriteLine($"Plaintext (decrypted in {decrypt.TotalMilliseconds} ms) -> {plain.Length} bytes");
                //int count = 0;
                //for (int i = 0; i < plain.Length; i++)
                //{
                //    Debug.Write($" {plain[i]:X2} ");
                //    if (++count > 15)
                //    {
                //        Debug.WriteLine("");
                //        count = 0;
                //    }
                //}
                //Debug.WriteLine("");
            }
            catch (CryptographicException cryptExc)
            {
                //Debug.WriteLine($"AESDecryptProcess Exception. {cryptExc.Message}\r\n");
                return;
            }
            catch (Exception exc)
            {
                Debug.WriteLine($"AESDecryptProcess Other Exception. {exc.Message}\r\n");
                return;
            }
        }

#endregion

#region MAVLINK

        private async Task<bool> InitMavLink()
        {
            var ReceivedData = new ManualResetEventSlim(false);
            var temp = new PacketReceivedDelegate((object sender, MavLinkPacketBase packet) =>
            {
                ReceivedData.Set();
            });

            MavLinkTransport.OnPacketReceived += temp;
            MavLinkTransport.Initialize();

            try
            {
                ReceivedData.Wait(new CancellationTokenSource(10000).Token);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("MavLink validator canceled.\r\n");

                MavLinkTransport.Dispose();

                return false;
            }

            MavLinkTransport.OnPacketReceived -= temp;
            MavLinkTransport.OnPacketReceived += MavLinkReceived;
            MavLinkTransport.OnPacketDiscarded += MavLinkDiscarded;

            //var timer = new System.Timers.Timer(1000);
            //timer.Elapsed += (object sender, System.Timers.ElapsedEventArgs e) => droppedPackets = 0;
            //timer.Start();

            if (await GetMavLinkStreams())
                return true;
            else
                return false;
        }

        private void MavLinkDiscarded(object sender, MavLinkPacketBase packet)
        {
            try
            {
                var crc = MavLinkPacketV20.GetPacketCrc(packet as MavLinkPacketV20);
                Debug.WriteLine($"Discarded MavLink Packet | Expected Checksum is [ {packet.Checksum1:X2} {packet.Checksum2:X2} ] but calculated is [ {crc & 0xFF:X2} {crc >> 8:X2} ]");
            }
            catch (Exception)
            {
                return;
            }
            return;
        }

        int totalPackets = 0;
        int droppedPackets = 0;
        ushort lastSeqNum = 0;

        private void MavLinkReceived(object sender, MavLinkPacketBase packet)
        {
            Task.Run(() =>
            {
                //Debug.WriteLine("");
                //Debug.WriteLine($"New {packet.Message} received. Processed in {msgprocess.TotalMilliseconds} ms since decryption.");

                totalPackets++;
                if (lastSeqNum + 1 < packet.PacketSequenceNumber)
                    droppedPackets += packet.PacketSequenceNumber - lastSeqNum;
                lastSeqNum = packet.PacketSequenceNumber;

                switch (packet.Message)
                {
                    case UasCommandAck CmdACK:
                        MavLinkCmdAck.Set();
                        break;
                    case UasSysStatus SysStat:
                        MavLinkCmdAck.Set();
                        SysStat.DropRateComm = (ushort)droppedPackets.Map(0, droppedPackets + totalPackets, 0, 10000);
                        //Debug.WriteLine($"Comm Dropped packets: {droppedPackets} out of {droppedPackets + totalPackets}");
                        totalPackets = 0;
                        droppedPackets = 0;
                        (MainPage as MainPage).UpdateUI(packet.Message);
                        break;
                    default:
                        MavLinkCmdAck.Set();
                        (MainPage as MainPage).UpdateUI(packet.Message);
                        break;
                }
            });
        }

        private Task<bool> GetMavLinkStreams()
        {
            UasCommandLong[] Commands = new UasCommandLong[]
            {
                new UasCommandLong()
                {
                    TargetSystem = 1,
                    TargetComponent = (byte)MavComponent.MavCompIdAutopilot1,
                    Command = MavCmd.SetMessageInterval,
                    Param1 = new UasHeartbeat().MessageId,
                    Param2 = 3000000
                },
                new UasCommandLong()
                {
                    TargetSystem = 1,
                    TargetComponent = (byte)MavComponent.MavCompIdAutopilot1,
                    Command = MavCmd.SetMessageInterval,
                    Param1 = new UasSysStatus().MessageId,
                    Param2 = 2000000
                },
                new UasCommandLong()
                {
                    TargetSystem = 1,
                    TargetComponent = (byte)MavComponent.MavCompIdAutopilot1,
                    Command = MavCmd.SetMessageInterval,
                    Param1 = new UasGlobalPositionInt().MessageId,
                    Param2 = 200000
                },
                new UasCommandLong()
                {
                    TargetSystem = 1,
                    TargetComponent = (byte)MavComponent.MavCompIdAutopilot1,
                    Command = MavCmd.SetMessageInterval,
                    Param1 = new UasAttitude().MessageId,
                    Param2 = 100000
                },
                //new UasCommandLong()
                //{
                //    TargetSystem = 1,
                //    TargetComponent = (byte)MavComponent.MavCompIdAutopilot1,
                //    Command = MavCmd.SetMessageInterval,
                //    Param1 = new UasAltitude().MessageId,
                //    Param2 = 100000
                //}
            };

            foreach (var command in Commands)
            {
                MavLinkCmdAck.Reset();
                MavLinkTransport.SendMessage(command);
                try
                {
                    MavLinkCmdAck.Wait(new CancellationTokenSource(5000).Token);
                    break;
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine("GetMavLinkStreams timedout waiting for MavLinkCmdAck.\r\n");

                    return Task.FromResult(false);
                }
            }

            return Task.FromResult(true);
        }

#endregion

#region AES

        internal void InitializeAES()
        {
            enc = Aes.Create();
            enc.Key = Encoding.ASCII.GetBytes("finalproject2022");

            dec = Aes.Create();
            dec.Key = Encoding.ASCII.GetBytes("finalproject2022");
        }

        internal byte[] EncryptAndGetCipherBytes(byte[] plaintext)
        {
            List<byte> msg_to_send = new List<byte>();
            enc.GenerateIV();

            // Create the streams used for encryption.
            using (var msEncrypt = new MemoryStream())
            {
                using (var csEncrypt = new CryptoStream(msEncrypt, enc.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    using (var swEncrypt = new BinaryWriter(csEncrypt))
                    {
                        //Write all data to the stream.
                        swEncrypt.Write(plaintext);
                    }
                }

                msg_to_send.AddRange(enc.IV);
                msg_to_send.AddRange(msEncrypt.ToArray());
            }

            return msg_to_send.ToArray();
        }

        internal byte[] DecryptAndGetPlainBytes(byte[] cipher)
        {
            dec.IV = cipher.Take(16).ToArray();

            var chip = cipher.Skip(16).ToArray();

            //if (consolemutex.WaitOne(0) && cipher.Length > 50)
            //{
            //    Debug.Write($"Key -> [ ");
            //    for (int i = 0; i < dec.Key.Length; i++)
            //    {
            //        Debug.Write($"{dec.Key[i]:X2}");
            //    }
            //    Debug.WriteLine(" ]");

            //    Debug.Write($"IV -> [ ");
            //    for (int i = 0; i < dec.IV.Length; i++)
            //    {
            //        Debug.Write($"{dec.IV[i]:X2}");
            //    }
            //    Debug.WriteLine(" ]");

            //    Debug.Write($"Ciphertext -> [ ");
            //    for (int i = 0; i < chip.Length; i++)
            //    {
            //        Debug.Write($"{chip[i]:X2}");
            //    }
            //    Debug.WriteLine(" ]");

            //    Debugger.Break();

            //    consolemutex.ReleaseMutex();
            //}

            // Create the streams used for decryption.
            using (var msDecrypt = new MemoryStream(chip))
            {
                using (var csDecrypt = new CryptoStream(msDecrypt, dec.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    using (var srDecrypt = new BinaryReader(csDecrypt))
                    {
                        // Read the decrypted bytes from the decrypting stream
                        return srDecrypt.ReadBytes(4096);
                    }
                }
            }
        }

        #endregion
    }
}
