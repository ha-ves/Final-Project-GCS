using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MavLinkNet;
using Xamarin.Forms;

namespace TugasAkhir_GCS
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        string _testString = "Testing 1 2 3 4";
        public string TestString { get { return _testString; } set { _testString = value; OnPropertyChanged("TestString"); } }

        public ImageSource ImgUsed { get { return ImageSource.FromResource("PigeonMobile_Xamarin_Cs.Resources.pigeon.png", typeof(App).Assembly); } }

        AesManaged enc, dec;

        TcpClient tcpClient;

        public MainPage()
        {
            BindingContext = this;
            InitializeComponent();

            AES_Method();
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
            MemoryStream txStream = new MemoryStream();

            UasAttitude asd = new MavLinkNet.UasAttitude();
            MavLinkPacket.GetPacketForMessage(asd, 0, 0, 0).Serialize(new BinaryWriter(txStream));

            Debug.WriteLine("");
            Debug.Write("Attitude MAVLink Message : ");
            txStream.ToArray().ToArray().ToList().ForEach(item => Debug.Write(item.ToString("x2")));
            Debug.WriteLine("");
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
