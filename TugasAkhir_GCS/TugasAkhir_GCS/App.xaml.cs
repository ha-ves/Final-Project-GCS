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
        
        public MavLinkGenericTransport MavLinkTransport;
        public ManualResetEventSlim MavLinkCmdAck = new ManualResetEventSlim(false);

        /* Transport variables */
        Aes enc, dec;

        // dummy mavlink 2.0 attitude msg
        public List<byte> DummyBuf = new List<byte>()
        { 0xfd, 0x10, 0x00, 0x00, 0x3a, 0x00, 0xc8, 0x1e, 0x00, 0x00, 0xbd, 0x12, 0x01, 0x00, 0x83, 0xa1,
            0xe9, 0xbb, 0xb4, 0x2d, 0x4b, 0xbc, 0x37, 0xd0, 0xaa, 0x3f, 0x0e, 0xa6 };

        /* stopwatch benchmark */
        public int seq = 0;
        public DateTime lastprocess;
        public TimeSpan decrypt, msgprocess, updateui;

        public IFileHandler writing;

        public App()
        {
            Thread.CurrentThread.Name = "MainApp Thread";

            InitializeComponent();

            MainPage = new MainPage();

            (MainPage as MainPage).HideLoadingOverlay();

            var file = $"Attitude_ProcessTime_{DateTime.Now.ToString("yyyy-MM-dddd_ss-mm-HH")}.csv";

            (App.Current as App).writing = DependencyService.Get<IFileHandler>();
            (App.Current as App).writing.Initialize(file);
        }

        #region Transport

        public async Task<bool> InitializeTransport()
        {
            writing.WriteLine("ATTITUDE (IMU SENSOR) PROCESSING DATA");
            writing.WriteLine("Seq,Decrypt Time (ms),Parse time (ms),UI update time (ms)");

            if (MavLinkTransport != null)
                MavLinkTransport.Dispose();

            MavLinkTransport = new MavLinkDefaultTransport();

            InitializeAES();

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

        private void AESDecryptProcess(object sender, byte[] data)
        {
            try
            {
                //decryption.Restart();
                lastprocess = DateTime.Now;

                var plain = (App.Current as App).DecryptAndGetPlainBytes(data);

                //decryption.Stop();
                //updateUI.Restart();

                decrypt = DateTime.Now - lastprocess;

                lastprocess = DateTime.Now;

                MavLinkTransport.DataReceived(sender, plain);

                Debug.WriteLine($"Plaintext (decrypted in {decrypt.TotalMilliseconds} ms) -> {plain.Length} bytes");
                int count = 0;
                for (int i = 0; i < plain.Length; i++)
                {
                    Debug.Write($" {plain[i]:X2} ");
                    if (++count > 15)
                    {
                        Debug.WriteLine("");
                        count = 0;
                    }
                }
                Debug.WriteLine("");
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

            //var timer = new System.Timers.Timer(1000);
            //timer.Elapsed += (object sender, System.Timers.ElapsedEventArgs e) => droppedPackets = 0;
            //timer.Start();

            if (await GetMavLinkStreams())
                return true;
            else
                return false;
        }

        int totalPackets = 0;
        int droppedPackets = 0;
        ushort lastSeqNum = 0;

        private void MavLinkReceived(object sender, MavLinkPacketBase packet)
        {
            Task.Run(() =>
            {
                msgprocess = DateTime.Now - lastprocess;

                lastprocess = DateTime.Now;

                Debug.WriteLine("");
                Debug.WriteLine($"New {packet.Message} received. Processed in {msgprocess.TotalMilliseconds} ms since decryption.");

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
                        SysStat.DropRateComm = (ushort)(droppedPackets * 10000 / totalPackets);
                        totalPackets = 0;
                        droppedPackets = 0;
                        (MainPage as MainPage).UpdateUI(packet.Message, lastprocess);
                        break;
                    default:
                        MavLinkCmdAck.Set();
                        (MainPage as MainPage).UpdateUI(packet.Message, lastprocess);
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

            // Create the streams used for decryption.
            using (var msDecrypt = new MemoryStream(cipher.Skip(16).ToArray()))
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
