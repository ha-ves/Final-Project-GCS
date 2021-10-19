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
using Xamarin.Forms;

namespace TugasAkhir_GCS
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        string _testString = "Testing 1 2 3 4";
        public string TestString { get { return _testString; } set { _testString = value; OnPropertyChanged("TestString"); } }

        public ImageSource ImgUsed { get { return ImageSource.FromResource("PigeonMobile_Xamarin_Cs.Resources.pigeon.png", typeof(App).Assembly); } }

        Aes aes;
        ICryptoTransform encryptor, decryptor;

        TcpClient tcpClient;

        public MainPage()
        {
            BindingContext = this;
            InitializeComponent();

            
        }

        private void AES_Method()
        {
            aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Key = Encoding.ASCII.GetBytes("testing123456789");
            aes.IV = Encoding.ASCII.GetBytes("testing123456789");

            encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            string plain = "test1234";
            string cipher64 = "";

            string cipher = "+kHv/IeW5ITSt2wXMEBGhnuc1fTLV6KbFQ0JIaVGxTc=";
            string decrypted = "";

            // Create the streams used for encryption.
            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        //Write all data to the stream.
                        swEncrypt.Write(plain);
                    }
                    Debug.WriteLine("");
                    Debug.WriteLine($"Encrypted Base64 : {Convert.ToBase64String(msEncrypt.ToArray())}");
                    Debug.Write($"Encrypted HEX : ");
                    msEncrypt.ToArray().ToList().ForEach(item => Debug.Write(item.ToString("x2")));
                    Debug.WriteLine("");
                }
            }

            // Create the streams used for decryption.
            using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(cipher)))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {

                        // Read the decrypted bytes from the decrypting stream
                        // and place them in a string.
                        decrypted = srDecrypt.ReadToEnd();
                        Debug.WriteLine("\n\nDecoded Base64 HEX : ");
                        Convert.FromBase64String(cipher).ToList().ForEach(item => Debug.Write(item.ToString("x2")));
                        Debug.WriteLine("");
                        Debug.WriteLine($"Decrypted : {decrypted}");
                        Debug.WriteLine("");
                    }
                }
            }
        }
    }
}
