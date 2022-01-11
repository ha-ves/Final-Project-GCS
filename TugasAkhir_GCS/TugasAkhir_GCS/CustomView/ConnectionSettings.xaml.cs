using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public Dictionary<string, object> Config;
    }

    public partial class ConnectionSettings : ContentView, INotifyPropertyChanged
    {
        public delegate void ConfirmedDelegate(object sender, ConnectionArgs ConnArgs);
        public event ConfirmedDelegate Confirmed;

        ConnectionArgs ConnArgs;

        ObservableCollection<string> _coms = new ObservableCollection<string>();
        public ObservableCollection<string> COMS { get { return _coms; } set { _coms = value; OnPropertyChanged("COMS"); } }

        string _selectedCom;
        public string SelectedCOM { get { return _selectedCom; } set { _selectedCom = value; OnPropertyChanged("SelectedCOM"); } }
        string _selectedBaud;
        public string SelectedBaud { get { return _selectedBaud; } set { _selectedBaud = value; OnPropertyChanged("SelectedBaud"); } }

        string _ipaddr = "192.168.4.1";
        public string IP { get { return _ipaddr; } set { _ipaddr = value; OnPropertyChanged("IP"); } }
        string _port = "61258";
        public string Port { get { return _port; } set { _port = value; OnPropertyChanged("Port"); } }

        public ConnectionSettings()
        {
            BindingContext = this;
            InitializeComponent();

            ((COM_Ports.Parent as StackLayout).Children[1] as Picker).SelectedIndex = 0;
        }

        private async void Btn_USB_Clicked(object sender, EventArgs e)
        {
            if ((App.Current as App).ReceiverService == null || (App.Current as App).ReceiverService.GetType() != DependencyService.Get<IReceiverService>().GetType())
            {
                if((App.Current as App).ReceiverService != null)
                    await (App.Current as App).ReceiverService.Disconnect();
                (App.Current as App).ReceiverService = DependencyService.Get<IReceiverService>(DependencyFetchTarget.NewInstance);
            }

            (App.Current.MainPage as MainPage).ShowLoadingOverlay("Refreshing...");

            Debug.WriteLine("USB BUTTON TAPPED");
            ConnArgs.ConnType = ConnectionType.USB;

            COMS.Clear();
            foreach (var com in await (App.Current as App).ReceiverService.RefreshSerialPorts())
                COMS.Add(com);

            if (COMS.Count == 0)
                COMS.Add("Tidak ada perangkat yang tersambung.");

            COM_Ports.SelectedIndex = 0;

            (sender as ImageButton).BorderWidth = 2;

            ConfigStack.IsVisible = true;
            (COM_Ports.Parent as View).IsVisible = true;

            (IP_Address.Parent as View).IsVisible = false;
            (((sender as ImageButton).Parent as StackLayout).Children[2] as ImageButton).BorderWidth = 0;

            (App.Current.MainPage as MainPage).HideLoadingOverlay();
        }

        private async void Btn_WIFI_Clicked(object sender, EventArgs e)
        {
            if ((App.Current as App).ReceiverService == null || (App.Current as App).ReceiverService.GetType() != typeof(WIFIService))
            {
                if ((App.Current as App).ReceiverService != null)
                    await (App.Current as App).ReceiverService.Disconnect();
                (App.Current as App).ReceiverService = new WIFIService();
            }

            Debug.WriteLine("WIFI BUTTON TAPPED");
            ConnArgs.ConnType = ConnectionType.WIFI;

            (sender as ImageButton).BorderWidth = 2;

            ConfigStack.IsVisible = true;
            (IP_Address.Parent as View).IsVisible = true;

            (COM_Ports.Parent as View).IsVisible = false;
            (((sender as ImageButton).Parent as StackLayout).Children[0] as ImageButton).BorderWidth = 0;
        }

        private void Confirm_Clicked(object sender, EventArgs e)
        {
            ConnArgs.Config = new Dictionary<string, object>();
            switch (ConnArgs.ConnType)
            {
                case ConnectionType.USB:
                    ConnArgs.Config.Add("COM", SelectedCOM);
                    ConnArgs.Config.Add("Baudrate", SelectedBaud);
                    break;
                case ConnectionType.WIFI:
                    ConnArgs.Config.Add("IP", IP);
                    ConnArgs.Config.Add("Port", Port);
                    break;
                default:
                    return;
            }
            Confirmed(this, ConnArgs);
            Close_Clicked(sender, e);
        }

        private void Close_Clicked(object sender, EventArgs e)
        {
            IsVisible = false;
            IsEnabled = false;
            (Parent as View).IsVisible = false;
        }

        internal void ClosePanel() => Close_Clicked(this, null);

        internal void ShowPanel()
        {
            IsVisible = true;
            IsEnabled = true;
            (Parent as View).IsVisible = true;
        }
    }
}