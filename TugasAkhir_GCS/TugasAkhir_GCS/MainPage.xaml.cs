using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
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
        string _testString = "Testing 1 2 3 4";
        public string TestString { get { return _testString; } set { _testString = value; OnPropertyChanged("TestString"); } }

        public ImageSource ImgUsed { get { return ImageSource.FromResource("PigeonMobile_Xamarin_Cs.Resources.pigeon.png", typeof(App).Assembly); } }

        string _connectBtn_Text = "Connect";
        public string ConnectBtn_Text { get { return _connectBtn_Text; } set { _connectBtn_Text = value; OnPropertyChanged("ConnectBtn_Text"); } }

        AesManaged enc, dec;

        MavLinkAsyncWalker mavlinkParser;

        TcpClient tcpClient;

        public Button TheTitleBar;

        public MainPage()
        {
            BindingContext = this;
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            new Thread(() => WiFiThreadEntry()).Start();

            DependencyService.Get<ITopBarService>().PrepareTopBar(TopBarGrid, TitleBar);

            //PrepareMap();

            //AES_Method();
        }

        private async void PrepareMap()
        {
            Debug.WriteLine("Preparing map");

            var perm = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (perm != PermissionStatus.Granted)
            {
                Debug.WriteLine("USER SHOULD ALLOW MANUALLY");

                if(await DisplayAlert("Function not allowed",
                    $"This function requires Location Permission and we detected that you manually denied access." +
                    $"Please go to your device's setting and allow the permission for this App.", "Go To Setting", "Deny"))
                    DependencyService.Get<IPermissionRequestService>().RequestManualPermission(new Permissions.LocationWhenInUse());
            }
            else
            {
                await DisplayAlert("Location access granted", "Thank your for allowing Location access","Continue");
                Map.UiSettings.MyLocationButtonEnabled = true;
                Map.MyLocationEnabled = true;
            }
        }

        private void WiFiThreadEntry()
        {
            Thread.CurrentThread.Name = "WIFI THREAD";

            while (true)
            {
                Debug.WriteLine($"{Thread.CurrentThread.Name} is currently Running.");
                Thread.Sleep(3000);
            }
        }

        private void AES_Method()
        {
            enc = new AesManaged();
            enc.Key = Encoding.ASCII.GetBytes("finalproject2022");

            dec = new AesManaged();
            dec.Key = Encoding.ASCII.GetBytes("finalproject2022");
        }

        private void TestMavLink(object sender, EventArgs e)
        {
            List<byte> rxBuf = new List<byte>(){ 0xfd, 0x10, 0x00, 0x00, 0x3a, 0x00, 0xc8, 0x1e, 0x00, 0x00, 0xbd, 0x12, 0x01, 0x00, 0x83, 0xa1, 0xe9, 0xbb, 0xb4, 0x2d, 0x4b, 0xbc, 0x37, 0xd0, 0xaa, 0x3f, 0x0e, 0xa6 };

            mavlinkParser = new MavLinkAsyncWalker();
            mavlinkParser.PacketReceived += MavlinkMessageReceived;
            mavlinkParser.PacketDiscarded += MavlinkMessageDiscarded;
            mavlinkParser.ProcessReceivedBytes(rxBuf.ToArray(), 0, rxBuf.Count);
        }

        private void MavlinkMessageDiscarded(object sender, MavLinkPacketBase packet)
        {
            Debug.WriteLine("New mavlink data discarded");
        }

        private void MavlinkMessageReceived(object sender, MavLinkPacketBase packet)
        {
            Debug.WriteLine("New mavlink data parsed");
            string str = "";
            switch (packet.Message)
            {
                case UasAttitude attitude:
                    str = $"new {packet.Message.GetType().Name} message with YPR: {attitude.Yaw * 180 / Math.PI:0.00} | {attitude.Pitch * 180 / Math.PI:0.00} | {attitude.Roll * 180 / Math.PI:0.00}";
                    Debug.WriteLine(str);
                    TestString = str;
                    break;
                default:
                    str = $"Mavlink {packet.Message.GetType().Name} message is not supported by this GCS";
                    Debug.WriteLine(str);
                    TestString = str;
                    break;
            }
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
                        TestString = srDecrypt.ReadToEnd();
                    }
                }
            }
        }
    }
}
