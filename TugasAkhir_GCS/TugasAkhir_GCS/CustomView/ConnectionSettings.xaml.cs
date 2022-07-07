using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TugasAkhir_GCS.Interfaces;
using Xamarin.Essentials;
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
        public delegate void ConnSettingDelegate(object sender, ConnectionArgs ConnArgs);
        public event ConnSettingDelegate Confirmed;

        ConnectionArgs ConnArgs;

        public ObservableRangeCollection<string> _coms = new ObservableRangeCollection<string>() { "Tidak ada perangkat yang tersambung." };
        public ObservableRangeCollection<string> COMS { get => _coms; set => _coms = value; }

        string _selectedCom;
        public string SelectedCOM { get => _selectedCom; set { _selectedCom = value; OnPropertyChanged("SelectedCOM"); } }

        string _selectedBaud;
        public string SelectedBaud { get => _selectedBaud; set { _selectedBaud = value; OnPropertyChanged("SelectedBaud"); } }

        string _ipaddr = "192.168.4.1";
        public string IP { get => _ipaddr; set { _ipaddr = value; OnPropertyChanged("IP"); } }

        string _port = "61258";
        public string Port { get => _port; set { _port = value; OnPropertyChanged("Port"); } }

        /* panning variables */
        double LastX, LastY;

        public ConnectionSettings()
        {
            BindingContext = this;
            InitializeComponent();

            BaudRates.SelectedIndex = 0;
            IP_Proto.SelectedIndex = 0;
        }

        private async void Btn_USB_Clicked(object sender, EventArgs e)
        {
            if ((App.Current as App).ReceiverService == null || (App.Current as App).ReceiverService.GetType() != DependencyService.Get<IReceiverService>().GetType())
            {
                if((App.Current as App).ReceiverService != null)
                    await (App.Current as App).ReceiverService.Disconnect();
                (App.Current as App).ReceiverService = DependencyService.Get<IReceiverService>();
            }

            (App.Current.MainPage as MainPage).ShowLoadingOverlay("Refreshing...");

            Debug.WriteLine("USB BUTTON TAPPED");
            ConnArgs.ConnType = ConnectionType.USB;

            var lastcoms = COMS.ToArray();

            var canceltoken = new CancellationTokenSource(5000);

            Debug.WriteLine($"COMS.Count == 0 [{COMS.Count == 0}] || lastcoms.SequenceEqual(COMS) [{lastcoms.SequenceEqual(COMS)}]");
            string[] ports;

            while (COMS.Count == 0 || lastcoms.SequenceEqual(COMS))
            {
                if (canceltoken.IsCancellationRequested) break;

                COMS.Clear();

                ports = await (App.Current as App).ReceiverService.RefreshSerialPorts(canceltoken.Token);

                COMS.AddRange(ports);

                Debug.WriteLine($"COMS.Count == 0 [{COMS.Count == 0}] || lastcoms.SequenceEqual(COMS) [{lastcoms.SequenceEqual(COMS)}]");
            }

            if (COMS.Count == 0)
                COMS.AddRange(lastcoms);

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

            //Debug.WriteLine("WIFI BUTTON TAPPED");
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

        private void PanGestureRecognizer_PanUpdated(object sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    break;
                case GestureStatus.Running:
                    var winwidth = (App.Current.MainPage as MainPage).Width;
                    var winheight = (App.Current.MainPage as MainPage).Height;

                    var edgex = Math.Abs(Width - (App.Current.MainPage as MainPage).Width) / 2.0;
                    var edgey = Math.Abs(Height - (App.Current.MainPage as MainPage).Height) / 2.0;

                    var transx = LastX + e.TotalX;
                    var transy = LastY + e.TotalY;

                    if (transx < -edgex) transx = -edgex;
                    else if (transx > edgex) transx = edgex;

                    if (transy < -edgey) transy = -edgey;
                    else if (transy > edgey) transy = edgey;

                    Debug.WriteLine($"edgex = {edgex}, edgey = {edgey}\r\n" +
                        $"transx = {transx}, transy = {transy}\r\n");

                    TranslationX = transx;
                    TranslationY = transy;
                    break;
                case GestureStatus.Completed:
                    LastX = TranslationX;
                    LastY = TranslationY;

                    Debug.WriteLine($"lastx = {LastX}, lasty = {LastY}");
                    break;
                case GestureStatus.Canceled:
                    break;
                default:
                    break;
            }
        }
    }
}