using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TugasAkhir_GCS.Interfaces;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TugasAkhir_GCS
{
    public enum ConnectionType
    {
        USB,
        WIFI,
    }

    public struct ConnectionArgs
    {
        public ConnectionType ConnType;
        public string Config;
    }

    [XamlCompilation(XamlCompilationOptions.Skip)]
    public partial class ConnectionSettings : ContentView, INotifyPropertyChanged
    {
        public delegate void ConfirmedDelegate(object sender, ConnectionArgs configs);
        public event ConfirmedDelegate Confirmed;

        ConnectionArgs config;

        public ConnectionSettings()
        {
            InitializeComponent();
        }

        private void Btn_USB_Clicked(object sender, EventArgs e)
        {
            Debug.WriteLine("USB BUTTON TAPPED");
            config.ConnType = ConnectionType.USB;

            ConfigStack.IsVisible = true;

            COM_Ports.IsVisible = true;
            IP_Address.IsVisible = false;

            DependencyService.Get<ISerialPortService>().ListSerialPorts();
        }

        private void Btn_WIFI_Clicked(object sender, EventArgs e)
        {
            Debug.WriteLine("WIFI BUTTON TAPPED");
            config.ConnType = ConnectionType.WIFI;

            ConfigStack.IsVisible = true;

            IP_Address.IsVisible = true;
            COM_Ports.IsVisible = false;
        }

        private void Confirm_Clicked(object sender, EventArgs e)
        {
            Confirmed(this, config);
            Close_Clicked(sender, e);
        }

        private void Close_Clicked(object sender, EventArgs e)
        {
            IsVisible = false;
        }
    }
}