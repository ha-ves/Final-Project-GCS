using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MavLinkNet;
using TugasAkhir_GCS.Interfaces;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.GoogleMaps;
using Xamarin.Forms.Xaml;

namespace TugasAkhir_GCS
{
    [XamlCompilation(XamlCompilationOptions.Skip)]
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        string _connectBtn_Text = "Connect";
        public string ConnectBtn_Text { get { return _connectBtn_Text; } set { _connectBtn_Text = value; OnPropertyChanged("ConnectBtn_Text"); } }

        AesManaged enc, dec;

        ConnectionArgs ConnectionArgs;

        MavLinkTCPClientTransport MavLinkTCPTransport;

        public MainPage()
        {
            InitializeComponent();

            BindingContext = this;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            new Thread(async () => await WIFIThreadEntry()).Start();

            DependencyService.Get<ITopBarService>().PrepareTopBar(TitleBar);
        }

        private void ConnSettings_Confirmed(object sender, ConnectionArgs configs)
        {
            
        }

        Channel<List<byte>> AESChannel = Channel.CreateUnbounded<List<byte>>();

        AutoResetEvent MavLinKConnValid = new AutoResetEvent(false);

        private async Task WIFIThreadEntry()
        {
            Thread.CurrentThread.Name = "WIFI THREAD";

            List<NetworkInterface> interfaces = new List<NetworkInterface>(NetworkInterface.GetAllNetworkInterfaces().Where(intf => intf.OperationalStatus == OperationalStatus.Up && (intf.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || intf.Name.Contains("wlan"))));
            if (interfaces.Count == 0)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("ERROR : Perangkat tidak didukung!", "Perangkat anda tidak memiliki WIFI, Aplikasi GCS tidak bisa dijalankan!\n", "OK");
                });
            }

            /*
            WIFIClient = new TcpClient();

            Debug.WriteLine($"Try Connecting to [ {gatewayIP.Address}:61258 ]");
            if (await Task.WhenAny(WIFIClient.ConnectAsync(gatewayIP.Address, 61258), Task.Delay(1000)) != null && WIFIClient.Connected)
            {
                Debug.WriteLine($"Connected to {WIFIClient.Client.RemoteEndPoint}");

                MavLinkTCPTransport = new MavLinkTCPClientTransport()
                {
                    Socket = WIFIClient.Client,
                    MavlinkSystemId = 255,
                    MavlinkComponentId = (byte)MavComponent.MavCompIdMissionplanner,
                    WireProtocolVersion = WireProtocolVersion.v10,
                    HeartBeatUpdateRateMs = 5000,
                };

                PacketReceivedDelegate p = null;
                p = delegate (object sender, MavLinkPacketBase packet)
                {
                    MavLinKConnValid.Set();
                    MavLinkTCPTransport.OnPacketReceived -= p;
                };

                MavLinkTCPTransport.OnPacketReceived += p;
                MavLinkTCPTransport.Initialize();

                if (MavLinKConnValid.WaitOne(5000))
                {
                    Debug.WriteLine("The MavLink Connection is Valid.");
                    MavLinkTCPTransport.OnPacketReceived += MavlinkMessageReceived;
                }
            }
            */

            while (true)
            {
                var data = await AESChannel.Reader.ReadAsync();

                Debug.Write($"new Data available :\nHEX -> ");
                data.ForEach(item =>
                {
                    Debug.Write($" {item:X2} ");
                });
                Debug.WriteLine(" |END");
            }
        }

        

        private void AES_Method()
        {
            enc = new AesManaged();
            enc.Key = Encoding.ASCII.GetBytes("finalproject2022");

            dec = new AesManaged();
            dec.Key = Encoding.ASCII.GetBytes("finalproject2022");
        }

        #region MAVLINK

        List<byte> DummyBuffer = new List<byte>() { 0xfd, 0x10, 0x00, 0x00, 0x3a, 0x00, 0xc8, 0x1e, 0x00, 0x00, 0xbd, 0x12, 0x01, 0x00, 0x83, 0xa1, 0xe9, 0xbb, 0xb4, 0x2d, 0x4b, 0xbc, 0x37, 0xd0, 0xaa, 0x3f, 0x0e, 0xa6 };

        private void MavlinkMessageReceived(object sender, MavLinkPacketBase packet)
        {
            Debug.WriteLine("New mavlink data parsed");

            switch (packet.Message)
            {
                case UasAttitude attitude:
                    Debug.WriteLine($"new {packet.Message.GetType().Name} message with YPR: {attitude.Yaw * 180 / Math.PI:0.00} | {attitude.Pitch * 180 / Math.PI:0.00} | {attitude.Roll * 180 / Math.PI:0.00}");
                    break;
                default:
                    Debug.WriteLine($"Mavlink {packet.Message.GetType().Name} message is not supported by this GCS");
                    break;
            }
        }

        private Task GetMavLinkStreams()
        {
            UasCommandLong UasCommand;

            UasCommand = new UasCommandLong()
            {
                TargetSystem = 1,
                TargetComponent = (byte)MavComponent.MavCompIdAutopilot1,
                Command = MavCmd.SetMessageInterval,
                Param1 = new UasHeartbeat().MessageId,
                Param2 = 2000000
            };

            MavLinkTCPTransport.SendMessage(UasCommand);

            UasCommand = new UasCommandLong()
            {
                TargetSystem = 1,
                TargetComponent = (byte)MavComponent.MavCompIdAutopilot1,
                Command = MavCmd.SetMessageInterval,
                Param1 = new UasSysStatus().MessageId,
                Param2 = 2000000
            };

            MavLinkTCPTransport.SendMessage(UasCommand);

            UasCommand = new UasCommandLong()
            {
                TargetSystem = 1,
                TargetComponent = (byte)MavComponent.MavCompIdAutopilot1,
                Command = MavCmd.SetMessageInterval,
                Param1 = new UasGlobalPositionInt().MessageId,
                Param2 = 1000000
            };

            MavLinkTCPTransport.SendMessage(UasCommand);

            UasCommand = new UasCommandLong()
            {
                TargetSystem = 1,
                TargetComponent = (byte)MavComponent.MavCompIdAutopilot1,
                Command = MavCmd.SetMessageInterval,
                Param1 = new UasAttitude().MessageId,
                Param2 = 200000
            };

            MavLinkTCPTransport.SendMessage(UasCommand);

            UasCommand = new UasCommandLong()
            {
                TargetSystem = 1,
                TargetComponent = (byte)MavComponent.MavCompIdAutopilot1,
                Command = MavCmd.SetMessageInterval,
                Param1 = new UasVfrHud().MessageId,
                Param2 = 200000
            };

            MavLinkTCPTransport.SendMessage(UasCommand);

            return Task.FromResult(true);
        }

        #endregion

        #region Button events

        private async void Connection_Clicked(object sender, EventArgs e)
        {
            await AESChannel.Writer.WriteAsync(DummyBuffer);
        }

        private void Toggle_ConnSettings(object sender, EventArgs e)
        {
            ConnSettings.IsVisible = !ConnSettings.IsVisible;
        }

        private void Button_Clicked(object sender, EventArgs e)
        { 
            // Prepare encryption
            string plain = "test1234";
            List<byte> cipher = new List<byte>();
            List<byte> msg_to_send = new List<byte>();
            enc.GenerateIV();

            // Create the streams used for encryption.
            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, enc.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        //Write all data to the stream.
                        swEncrypt.Write(plain);
                    }
                    cipher = msEncrypt.ToArray().ToList();
                    msg_to_send.AddRange(enc.IV);
                    msg_to_send.AddRange(cipher);
                }
            }

            // Prepare for encryption
            dec.IV = msg_to_send.Take(16).ToArray();

            // Create the streams used for decryption.
            using (MemoryStream msDecrypt = new MemoryStream(msg_to_send.Skip(16).ToArray()))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, dec.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        // Read the decrypted bytes from the decrypting stream
                        // and place them in a string.
                        
                    }
                }
            }
        }

        #endregion

        #region unused

        private async void PrepareMap()
        {
            Debug.WriteLine("Preparing map");

            var perm = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (perm != PermissionStatus.Granted)
            {
                Debug.WriteLine("USER SHOULD ALLOW MANUALLY");

                if (await DisplayAlert("Function not allowed",
                    $"This function requires Location Permission and we detected that you manually denied access." +
                    $"Please go to your device's setting and allow the permission for this App.", "Go To Setting", "Deny"))
                    DependencyService.Get<IPermissionRequestService>().RequestManualPermission(new Permissions.LocationWhenInUse());
            }
            else
            {
                await DisplayAlert("Location access granted", "Thank your for allowing Location access", "Continue");
                Map.UiSettings.MyLocationButtonEnabled = true;
                Map.MyLocationEnabled = true;
            }
        }

        #endregion
    }
}
