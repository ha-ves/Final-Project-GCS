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
using Accord.Fuzzy;
using MavLinkNet;
using TugasAkhir_GCS.CustomView;
using TugasAkhir_GCS.Interfaces;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TugasAkhir_GCS
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        /* Flight data variables */
        string _flightMode = "offline";
        public string FlightMode { get => _flightMode; set { _flightMode = value; OnPropertyChanged("FlightMode"); } }

        //string _battPercent = "baterai : n/a %";
        string _battPercent = AppResources. + " : n/a %";
        public string BattPercent { get => _battPercent; set { _battPercent = value; OnPropertyChanged("BattPercent"); } }

        string _signalPercent = "sinyal : n/a %";
        public string SignalPercent { get => _signalPercent; set { _signalPercent = value; OnPropertyChanged("SignalPercent"); } }

        string _flightTime = "waktu terbang : t+00:00:00.00";
        public string FlightTime { get => _flightTime; set { _flightTime = value; OnPropertyChanged("FlightTime"); } }

        /* timers n stuff */
        System.Timers.Timer FlightTimer;
        Stopwatch FlightStopwatch;

        /* Kestabilan terbang variables */
        List<double> LastSamples = new List<double>();
        List<double> LastSamplesTime = new List<double>();
        const int SamplesWindow = 10;

        /* Other variables */
        bool useCompass = false;

        public MainPage()
        {
            BindingContext = this;
            InitializeComponent();

            test.Elapsed += Elapsed;
        }

        #region Loading Overlay Control

        public void ShowLoadingOverlay(string message = "")
        {
            LoadingOverlay.IsVisible = true;
            LoadingAct.IsRunning = true;
            LoadingMessage.Text = message;
        }

        public void ChangeLoadingOverlay(string message)
        {
            LoadingMessage.Text = message;
        }

        public void HideLoadingOverlay()
        {
            LoadingOverlay.IsVisible = false;
            LoadingAct.IsRunning = false;
            LoadingMessage.Text = "";
        }

        public void ToggleWaktuKembali(bool bul) => MainThread.InvokeOnMainThreadAsync(() => WaktuKembali.IsVisible = bul);

        #endregion

        #region Update UI

        DateTime lastdatetime = DateTime.Now;

        public void UpdateUI(UasMessage msg)
        {
#if DATA_FETCH
            TimeSpan updateui;
#endif
            switch (msg)
            {
                case UasSysStatus SysStat:
                    UpdateBatt(SysStat.BatteryRemaining);
                    UpdateSignal(SysStat.DropRateComm);
                    //(App.Current as App).ReturnTime.ReturnTimeUpdate();
#if DATA_FETCH
                    updateui = DateTime.Now - (App.Current as App).packettime;

                    (App.Current as App).syssavefile.WriteLine(
                        //$"{(App.Current as App).decrypt.TotalMilliseconds}," +
                        //$"{msgprocess.TotalMilliseconds}," +
                        $"{updateui.TotalMilliseconds}");
#endif
                    break;
                case UasGlobalPositionInt Gps:
                    UpdateGPS(Gps.Lat, Gps.Lon, Gps.RelativeAlt);
                    UpdateBearing(Gps.Hdg);
                    Alti_Avionic.UpdateUI(Gps.RelativeAlt);
                    (App.Current as App).ReturnTime.ReturnTimeUpdate();
#if DATA_FETCH
                    updateui = DateTime.Now - (App.Current as App).packettime;

                    (App.Current as App).gpssavefile.WriteLine(
                        //$"{(App.Current as App).decrypt.TotalMilliseconds}," +
                        //$"{msgprocess.TotalMilliseconds}," +
                        $"{updateui.TotalMilliseconds}");
#endif
                    break;
                case UasAttitude Att:
                    double grads = 0;
                    UpdateAtt(Att.Yaw, Att.Pitch, Att.Roll);
                    UpdateKestabilanTerbang(out grads);
#if DATA_FETCH
                    (App.Current as App).stabilfile.WriteLine($"{Att.Roll}," +
                        $"{grads}");

                    updateui = DateTime.Now - (App.Current as App).packettime;
                        
                    (App.Current as App).attsavefile.WriteLine(
                        //$"{(App.Current as App).decrypt.TotalMilliseconds}," +
                        //$"{msgprocess.TotalMilliseconds}," +
                        $"{updateui.TotalMilliseconds}");
#endif
                    break;
                case UasHeartbeat HrtBt:
                    UpdateFlightMode(HrtBt.SystemStatus);
#if DATA_FETCH
                    updateui = DateTime.Now - (App.Current as App).packettime;

                    (App.Current as App).hrtsavefile.WriteLine(
                        //$"{(App.Current as App).decrypt.TotalMilliseconds}," +
                        //$"{msgprocess.TotalMilliseconds}," +
                        $"{updateui.TotalMilliseconds}");
#endif
                    break;
                default:
                    return;
            }

#if DATA_FETCH
            Debug.WriteLine($"UI {msg} updated in {updateui.TotalMilliseconds} ms");
#endif
        }

        internal void UpdateWaktuKembali(double result)
        {
            var slider = (((WaktuKembali.Content as StackLayout).Children.First(view => view.GetType() == typeof(Grid)) as Grid).Children.First(view => view.GetType() == typeof(Slider)) as Slider).Value;
            
            new Animation(start: slider, end: 10.0 - result, easing: Easing.CubicOut,
                callback: val =>
                    (((WaktuKembali.Content as StackLayout).Children.First(view => view.GetType() == typeof(Grid)) as Grid).Children.First(view => view.GetType() == typeof(Slider)) as Slider).Value = val)
                .Commit(this, "SliderAnim", length: 1000);

            var lumino = (Math.Log10(1 + result) / Math.Log10(11)).Map(0.0, 1.3, 0.3, 0.5);
            var hue = 0.35 - result.Map(0.0, 10.0, 0.0, 0.45);

            var warningcol = (WaktuKembali.Content as StackLayout).Children.First(view => view.GetType() == typeof(Frame)).BackgroundColor;

            var nextcol = Color.Green.WithHue(hue).WithLuminosity(lumino);

            AnimationExtensions.Animate(this, name: "WarningAnim", easing: Easing.CubicOut, length: 1000,
                transform: (time) =>
                {
                    var R = warningcol.R + (nextcol.R - warningcol.R) * time;
                    var G = warningcol.G + (nextcol.G - warningcol.G) * time;
                    var B = warningcol.B + (nextcol.B - warningcol.B) * time;
                    var A = warningcol.A + (nextcol.A - warningcol.A) * time;

                    return new Color(R, G, B, A);
                },
                callback: (val) => (WaktuKembali.Content as StackLayout).Children.First(view => view.GetType() == typeof(Frame)).BackgroundColor = val);
        }

        internal void WaktuKembaliBlinker()
        {
            var oricolor = Color.FromHex("#af000000");
            var color = (WaktuKembali.Content as StackLayout).Children.First(view => view.GetType() == typeof(Frame)).BackgroundColor;

            AnimationExtensions.Animate(this, name: "BlinkAnim", easing: Easing.CubicOut, length: 500,
                transform: (time) =>
                {
                    var R = oricolor.R + (color.R - oricolor.R) * time;
                    var G = oricolor.G + (color.G - oricolor.G) * time;
                    var B = oricolor.B + (color.B - oricolor.B) * time;
                    var A = oricolor.A + (color.A - oricolor.A) * time;

                    return new Color(R, G, B, A);
                },
                callback: (val) => WaktuKembali.BackgroundColor = val,
                finished: (col, isfinish) =>
                {
                    AnimationExtensions.Animate(this, name: "UnblinkAnim", easing: Easing.CubicIn, length: 500,
                        transform: (time) =>
                        {
                            var R = color.R + (oricolor.R - color.R) * time;
                            var G = color.G + (oricolor.G - color.G) * time;
                            var B = color.B + (oricolor.B - color.B) * time;
                            var A = color.A + (oricolor.A - color.A) * time;

                            return new Color(R, G, B, A);
                        },
                        callback: (val) => WaktuKembali.BackgroundColor = val);
                });
        }

        private void UpdateFlightMode(MavState state)
        {
#if DATA_FETCH
            MainThread.BeginInvokeOnMainThread(() => fmode.Text = Enum.GetName(typeof(MavState), state));
#else
            FlightMode = Enum.GetName(typeof(MavState), state);
#endif
        }

        private void UpdateAtt(float yawRad, float pitchRad, float rollRad)
        {
            LastSamples.Add(rollRad * 180.0 / Math.PI);
            //if (FlightStopwatch != null)
            //    LastSamplesTime.Add(FlightStopwatch.Elapsed.TotalMilliseconds);

            if (LastSamples.Count > SamplesWindow)
                LastSamples.RemoveAt(0);
            //if (LastSamplesTime.Count > SamplesWindow)
            //    LastSamplesTime.RemoveAt(0);

            IMU_Avionic.UpdateUI(pitchRad, rollRad);

            if (!useCompass)
                MapView.UpdateBearing((float)(((yawRad * 180.0 / Math.PI) + 360.0) % 360));
        }

#if USE_FIT_LINE
        readonly double[] window = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
#endif
        private void UpdateKestabilanTerbang(out double grads)
        {
            if (LastSamples.Count < SamplesWindow /*|| LastSamplesTime.Count < SamplesWindow*/)
            {
                grads = 0;
                return;
            }
#if USE_FIT_LINE
            var grad = MathNet.Numerics.Fit.Line(window, LastSamples.ToArray()).Item2;
            grads = grad;
#else
            var fitline = MathNet.Numerics.Fit.Line(window, LastSamples.ToArray()).Item2;

            int sigmaX = 0;
            int sigma_XSquare = 0;

            var sigmaY = LastSamples.Sum();
            var sigmaXY = 0d;

            for (int i = 0; i < SamplesWindow; i++)
            {
                sigmaX += i + 1;
                sigma_XSquare += (i + 1) * (i + 1);
                sigmaXY += (i + 1) * LastSamples[i];
            }

            var grad = ((SamplesWindow * sigmaXY) - (sigmaX * sigmaY)) / ((SamplesWindow * sigma_XSquare) - (sigmaX * sigmaX));

            //Debug.WriteLine($"Kestabilan fit line = {fitline:0.######} | m = {grad:0.######}\r\n" +
            //    $"diff = {fitline-grad:0.######}");
#endif
            var m = Math.Abs(grad);
            
            MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (m >= 5)
                {
                    Kestabilan.IsVisible = true;
                    Kestabilan.BackgroundColor = Color.Red;
                    (Kestabilan.Content as Label).Text = "Sangat miring";
                    if (grad < 0)
                        (Kestabilan.Content as Label).Text += " Ke Kiri";
                    else
                        (Kestabilan.Content as Label).Text += " Ke Kanan";
                    (Kestabilan.Content as Label).TextColor = Color.White;

                    //Debug.WriteLine((Kestabilan.Content as Label).Text);
                    //Debug.WriteLine("Last samples :");
                    //for (int i = 0; i < samples; i++)
                    //{
                    //    Debug.WriteLine($"{LastSamplesTime[i]} | {LastSamples[i]} ");
                    //}
                    //Debug.WriteLine("");
                }
                else if (1 < m && m < 5)
                {
                    Kestabilan.IsVisible = true;
                    Kestabilan.BackgroundColor = Color.Yellow;
                    (Kestabilan.Content as Label).Text = "Miring";
                    if (grad < 0)
                        (Kestabilan.Content as Label).Text += " Ke Kiri";
                    else
                        (Kestabilan.Content as Label).Text += " Ke Kanan";
                    (Kestabilan.Content as Label).TextColor = Color.Black;

                    //Debug.WriteLine((Kestabilan.Content as Label).Text);
                    //Debug.WriteLine("Last samples :");
                    //for (int i = 0; i < samples; i++)
                    //{
                    //    Debug.WriteLine($"{LastSamplesTime[i]} | {LastSamples[i]} ");
                    //}
                    //Debug.WriteLine("");
                }
                else
                    Kestabilan.IsVisible = false;
            })
#if DATA_FETCH
            .Wait();
#else
            ;
#endif
        }

        private void UpdateGPS(int lat, int lon, int alt)
        {
            //lat = -74107080 + new Random().Next(-200, 200);
            //lon = 1127047190 + new Random().Next(-200, 200);

            if (lat == int.MinValue || lon == int.MinValue || lat == 0 || lon == 0)
                return;

            MapView.UpdateGPS(lat, lon, alt);
        }

        private void UpdateBearing(ushort hdg)
        {
            if (hdg == ushort.MaxValue || !useCompass)
                return;

            MapView.UpdateBearing((float)((hdg - 9000 + 36000) % 36000 * 0.01));
            Bearing_Avionic.UpdateUI((float)(hdg * 0.01));
        }

        private void UpdateBatt(sbyte batteryRemaining)
        {
            //(App.Current as App).ReturnTime.UAVBatt = batteryRemaining;
#if DATA_FETCH
            MainThread.BeginInvokeOnMainThread(() => batt.Text = $"batt : {batteryRemaining} %");
#else
            BattPercent = $"batt : {batteryRemaining} %";
#endif
        }

        private void UpdateSignal(ushort dropRateComm)
        {
#if DATA_FETCH
            MainThread.BeginInvokeOnMainThread(() => sig.Text = $"rssi : {100.0 - (dropRateComm / 100.0):0.00} %");
#else
            SignalPercent = $"sinyal : {100.0 - (dropRateComm * 0.01):0.00} %";
            Debug.WriteLine(SignalPercent);
#endif
        }

        #endregion

        #region Button events

        private async void Button_Clicked_3(object sender, EventArgs e)
        {
            ShowLoadingOverlay("Doing randomize FIS return time");

            await Task.Run(() =>
            {
                (App.Current as App).ReturnTime = new ReturnTimeService();

                Debug.WriteLine("Start return time randomizer (BATT, ALT, OUTPUT)");

                (App.Current as App).returnfile.Write(",");

                var samples = 100;

                // column BATT
                for (int i = 0; i <= samples; i++)
                {
                    decimal col = ((decimal)(100.0 * i / samples));
                    if (col % 1 == 0)
                        (App.Current as App).returnfile.Write(col.ToString() + ',');
                    else
                        (App.Current as App).returnfile.Write(",");
                }
                (App.Current as App).returnfile.WriteLine(",");

                // for each rows ALT
                for (int i = 0; i <= samples; i++)
                {
                    decimal row = ((decimal)(400.0 * i / samples));
                    if (row % 1 == 0)
                        (App.Current as App).returnfile.Write(row.ToString() + ',');
                    else
                        (App.Current as App).returnfile.Write(",");

                    // for each column BATT
                    for (int j = 0; j <= samples; j++)
                    {
                        var res = (App.Current as App).ReturnTime.ReturnTimeUpdate(/*100.0 * j / samples, 2.5, 400.0 * i / samples*/);
                        (App.Current as App).returnfile.Write(res.ToString() + ',');
                    }

                    (App.Current as App).returnfile.WriteLine("");
                }

                Debug.WriteLine("End return time randomizer");

                (App.Current as App).FinishDataFetch();
            });

            HideLoadingOverlay();
        }

        private void Button_Clicked_2(object sender, EventArgs e)
        {
            (App.Current as App).ErrorMavlink = true;
        }

        private void Collapsible_Clicked(object sender, EventArgs e)
        {
            var lbl = sender as Label;
            var avionics = (lbl.Parent as StackLayout).Children.First(view => view.GetType() == typeof(StackLayout));
            var botpanel = (lbl.Parent.Parent.Parent as VisualElement);
            var botpaneltransy = App.Current.Resources["BotPanelCollapsedTransY"] as OnIdiom<short>;

            if (avionics.IsVisible)
            {
                new Animation(start: 0, end: botpaneltransy, callback: val => botpanel.TranslationY = val)
                    .Commit(this, "CollapseAnim", length: App.Current.Resources["AnimLength"] as OnIdiom<byte>,
                    finished: (val, isfinish) =>
                    {
                        avionics.IsVisible = false;
                        lbl.Text = "Tekan to membuka ▲";
                        botpanel.TranslationY = 0;
                    });
            }
            else
            {
                avionics.IsVisible = true;
                botpanel.TranslationY = (double)botpaneltransy;

                new Animation(start: botpaneltransy, end: 0, callback: val => botpanel.TranslationY = val)
                    .Commit(this, "ExpandAnim", length: App.Current.Resources["AnimLength"] as OnIdiom<byte>,
                    finished: (val, isfinish) => lbl.Text = "Tekan to menutup ▼");
            }
        }

        private void ConnSettings_Confirmed(object sender, ConnectionArgs ConnArgs)
        {
            (App.Current as App).CurrentConnection = ConnArgs;
            ((View)ConnectBtn).IsEnabled = true;
            ((View)ConnectBtn).BackgroundColor = Color.Red;
        }

        private void Toggle_ConnSettings(object sender, EventArgs e)
        {
            if (!ConnSettings.IsVisible)
                ConnSettings.ShowPanel();
            else
                ConnSettings.ClosePanel();
        }

        private async void Connection_Clicked(object sender, EventArgs e)
        {
            if (!(App.Current as App).IsConnected)
            {
                ((View)ConnSettingBtn).IsEnabled = false;
                
                (sender as Button).Text = "Menghubungkan...";
                (sender as Button).IsEnabled = false;
                (sender as Button).BackgroundColor = Color.DarkGray;

                ShowLoadingOverlay("Menyambungkan UAV . . .");

                switch ((App.Current as App).CurrentConnection.ConnType)
                {
                    case ConnectionType.USB:
                        var COM = (App.Current as App).CurrentConnection.Config["COM"] as string;

                        if (!await Task.Run(() => (App.Current as App).ReceiverService.ConnectTo(COM, (App.Current as App).CurrentConnection.Config["Baudrate"] as string)))
                            break;

                        (App.Current as App).IsConnected = true;
                        break;
                    case ConnectionType.WIFI:
                        if (await Permissions.RequestAsync<Permissions.NetworkState>() != PermissionStatus.Granted)
                            return;

                        var IP = (App.Current as App).CurrentConnection.Config["IP"] as string;
                        var Port = (App.Current as App).CurrentConnection.Config["Port"] as string;
                        ushort PortNum = 0;

                        if (IP.Count(dot => dot == '.') < 3)
                        {
                            if (!await DisplayAlert("IP Address tidak valid :", (IP == "") ? "<Tidak ada IP Address>" : IP, "OK", "Ubah"))
                                ConnSettings.ShowPanel();
                            break;
                        }
                        else if (!ushort.TryParse(Port, out PortNum))
                        {
                            if (!await DisplayAlert("Port tidak valid :", (Port == "") ? "<Tidak ada Port>" : Port, "OK", "Ubah"))
                                ConnSettings.ShowPanel();
                            break;
                        }

                        if (!await Task.Run(() => ((App.Current as App).ReceiverService as WIFIService).ConnectTo(IP, PortNum)))
                            break;

                        (App.Current as App).IsConnected = true;
                        break;
                    default:
                        break;
                }

                if ((App.Current as App).IsConnected)
                {
                    StartFlightTimer();

                    (sender as Button).Text = "tekan untuk putuskan";
                    (sender as Button).BackgroundColor = Color.Green;
                }
                else
                {
                    await DisplayAlert("Gagal terhubung ke UAV", "Silakan coba lagi.", "OK");

                    (sender as Button).Text = "tekan untuk koneksi";
                    (sender as Button).BackgroundColor = Color.Red;

                    ((View)ConnSettingBtn).IsEnabled = true;
                }
                (sender as Button).IsEnabled = true;
            }
            else
            {
                if (!await DisplayAlert("Anda akan memutuskan koneksi UAV", "Anda menekan tombol Disconnect, apakah anda yakin?", "YA, DISCONNECT", "Batal"))
                    return;

                (sender as Button).Text = "Memutuskan...";
                (sender as Button).IsEnabled = false;
                (sender as Button).BackgroundColor = Color.DarkBlue;

                ShowLoadingOverlay("Memutuskan koneksi UAV . . .");

                (App.Current as App).FinishDataFetch();

                if (!await (App.Current as App).ReceiverService.Disconnect())
                    return;
                if ((App.Current as App).MavLinkTransport != null)
                    (App.Current as App).MavLinkTransport.Dispose();

                (App.Current as App).IsConnected = false;

                if ((App.Current as App).IsConnected)
                {
                    (sender as Button).Text = "tekan untuk putuskan";
                    (sender as Button).BackgroundColor = Color.Green;
                }
                else
                {
                    StopFlightTimer();
                    (App.Current as App).ReturnTime.Dispose();

                    (sender as Button).Text = "tekan untuk koneksi";
                    (sender as Button).BackgroundColor = Color.Red;

                    ((View)ConnSettingBtn).IsEnabled = true;
                }
                (sender as Button).IsEnabled = true;
            }

            HideLoadingOverlay();
        }

        private void StartFlightTimer()
        {
            FlightStopwatch = Stopwatch.StartNew();

            FlightTimer = new System.Timers.Timer(1 / 30.0);
            FlightTimer.Elapsed += (sender, e) =>
            {
                FlightTime = "waktu terbang : t+" + FlightStopwatch.Elapsed.ToString("hh\\:mm\\:ss\\.ff");
            };
            FlightTimer.Start();
        }

        private void StopFlightTimer()
        {
            if (FlightStopwatch.IsRunning)
                FlightStopwatch.Stop();

            if(FlightTimer != null)
            {
                FlightTimer.Stop();
                FlightTimer.Dispose();
                FlightTimer = null;
            }
        }

        #endregion

        #region Demo

        #region Demo AES



        #endregion

        #endregion

        #region FIS Test Area

        double _returntime = 0;
        public double ReturnTime { get => _returntime; set { _returntime = value; OnPropertyChanged("ReturnTime"); UpdateWaktuKembali(value); } }

        public sbyte GetBattMod() => (sbyte)BattMod.Value;
        public int GetJarakMod() => (int)JarakMod.Value;
        public int GetTinggiMod() => (int)TinggiMod.Value;

        #endregion

        #region AES Testing

        private async void Button_Clicked(object sender, EventArgs e)
        {
            ShowLoadingOverlay("Testing AES");

            await Task.Run(() =>
            {
                Thread.CurrentThread.Name = "AES Test Thread";

                (App.Current as App).InitAES();

                var timer = Stopwatch.StartNew();

                for (int i = 0; i < 1000000; i++)
                {
                    (App.Current as App).EncryptAndGetCipherBytes(Variables.DummyBuf.ToArray());
                }

                timer.Stop();
                Debug.WriteLine($"1 mil Encryption took {timer.ElapsedMilliseconds} ms / {timer.ElapsedTicks} ticks");

                var dummyCipher = (App.Current as App).EncryptAndGetCipherBytes(Variables.DummyBuf.ToArray());

                timer = Stopwatch.StartNew();

                for (int i = 0; i < 1000000; i++)
                {
                    (App.Current as App).DecryptAndGetPlainBytes(dummyCipher);
                }

                timer.Stop();
                Debug.WriteLine($"1 mil Decryption took {timer.ElapsedMilliseconds} ms / {timer.ElapsedTicks} ticks");

                var decrypted = (App.Current as App).DecryptAndGetPlainBytes(dummyCipher);

                Debug.Write("Decrypted data -> [");
                foreach (var item in decrypted)
                {
                    Debug.Write($" {item:X2} ");
                }
                Debug.WriteLine("]");
            });

            HideLoadingOverlay();
        }

        #endregion

        #region MavLink Dump

        private async void Button_Clicked_1(object sender, EventArgs e)
        {
            ShowLoadingOverlay($"Dumping MavLink AES...");

            var timers = new List<Timer>()
            {
                // heartbeat
                new Timer((state) =>
                {
                    var msg = new UasHeartbeat()
                    {
                        Type = MavType.Quadrotor,
                        Autopilot = MavAutopilot.Generic,
                        BaseMode = MavModeFlag.SafetyArmed | MavModeFlag.ManualInputEnabled | MavModeFlag.StabilizeEnabled,
                        SystemStatus = MavState.Standby,
                    };
                    (App.Current as App).MavLinkTransport.SendMessage(msg);
                }, null, 0, 2000),

                //sys status
                new Timer((state) =>
                {
                    var battmv = new Random().Next(11500, 12600);
                    (App.Current as App).MavLinkTransport.SendMessage(new UasSysStatus()
                    {
                        OnboardControlSensorsPresent = 0,
                        OnboardControlSensorsEnabled = 0,
                        OnboardControlSensorsHealth = 0,
                        Load = (ushort)new Random().Next(0, 1000),
                        VoltageBattery = (ushort)battmv,
                        CurrentBattery = -1,
                        BatteryRemaining = (sbyte)(battmv / 12600 * 100),
                        DropRateComm = 0,
                        ErrorsComm = 0,
                        ErrorsCount1 = 0,
                        ErrorsCount2 = 0,
                        ErrorsCount3 = 0,
                        ErrorsCount4 = 0,
                    });
                }, null, 0, 2000),

                //gps
                new Timer((state) =>
                {
                    (App.Current as App).MavLinkTransport.SendMessage(new UasGlobalPositionInt()
                    {
                        TimeBootMs = 1,
                        Lat = -74107080,
                        Lon = 1127047190,
                        Alt = new Random().Next(5800, 6200),
                        RelativeAlt = new Random().Next(90, 110),
                        Vx = 0,
                        Vy = 0,
                        Vz = 0,
                        Hdg = UInt16.MaxValue
                    });
                }, null, 0, 2000),

                //attitude
                new Timer((state) =>
                {
                    (App.Current as App).MavLinkTransport.SendMessage(new UasAttitude()
                    {
                        TimeBootMs = 1,
                        Roll = (float)(new Random().Next(-100, 100) / 36000.0 * Math.PI),
                        Pitch = (float)(new Random().Next(-100, 100) / 36000.0 * Math.PI),
                        Yaw = (float)(new Random().Next(-100, 100) / 36000.0 * Math.PI),
                        Rollspeed = 0,
                        Pitchspeed = 0,
                        Yawspeed = 0,
                    });
                }, null, 0, 100),
            };

            for (int i = 10 - 1; i >= 0; i--)
            {
                await Task.Delay(1000);
                ChangeLoadingOverlay($"Dumping MavLink AES, {i}s..");
            }

            timers.ForEach(timer =>
            {
                timer.Change(Timeout.Infinite, Timeout.Infinite);
                timer.Dispose();
            });

            HideLoadingOverlay();
        }

        #endregion

        #region Dummy Update UI

        System.Timers.Timer test = new System.Timers.Timer(1000);
        bool Stopped = true;

        private void DemoVisualisasi(object sender, EventArgs e)
        {
            if (Stopped)
            {
                test.Start();
                Stopped = false;
                useCompass = false;
            }
            else
            {
                test.Stop();
                Stopped = true;
                useCompass = true;
            }
        }

        void Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            MainThread.InvokeOnMainThreadAsync(() =>
            {
                var y = new Random().Next(-180, 180);
                var p = new Random().Next(-20, 20);
                var r = new Random().Next(-20, 20);

                var lat = -74107080 + new Random().Next(-1000, 1000);
                var lon = 1127047190 + new Random().Next(-1000, 1000);

                var batt = (sbyte)(80 + new Random().Next(-3, 3));

                DemoLabel.Text = "Demo Visualisasi :\r\n" +
                                $"Yaw : {y}\r\n" +
                                $"Pitch : {p}\r\n" +
                                $"Roll : {r} \r\n\n" +
                                $"Latitude : {lat/10000000.0}\r\n" +
                                $"Longitude : {lon/10000000.0} \r\n\n" +
                                $"Kapasitas Baterai : {batt} \r\n\n";

                UpdateAtt((float)(y * Math.PI / 180.0), (float)(p * Math.PI / 180.0), (float)(r * Math.PI / 180.0));
                //UpdateKestabilanTerbang();

                UpdateGPS(lat, lon, 0);

                UpdateBatt(batt);
            });
        }

        #endregion

    }
}
