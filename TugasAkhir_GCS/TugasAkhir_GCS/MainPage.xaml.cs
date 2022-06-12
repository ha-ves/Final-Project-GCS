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
using Xamarin.Forms.Xaml;

namespace TugasAkhir_GCS
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        /* Connection variables */
        ConnectionArgs CurrentConnection;
        bool IsConnected = false;

        /* Flight data variables */
        string _flightMode = "offline";
        public string FlightMode { get => _flightMode; }

        string _battPercent = "batt : n/a %";
        public string BattPercent { get => _battPercent; }

        string _signalPercent = "rssi : n/a %";
        public string SignalPercent { get => _signalPercent; }

        string _flightTime = "t+00:00:00.00";
        public string FlightTime { get => _flightTime; }

        System.Timers.Timer FlightTimer;
        Stopwatch FlightStopwatch;

        /* Kestabilan terbang variables */
        List<double> LastSamples = new List<double>();
        List<double> LastSamplesTime = new List<double>();
        const int samples = 10;

        /* Other variables */
        bool useCompass = true;

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

        #endregion

        #region Update UI

        public void UpdateUI(UasMessage msg, TimeSpan msgprocess, DateTime lastprocess)
        {
            //Debug.WriteLine($"New {msg}");

            var updateui = DateTime.Now - lastprocess;

            switch (msg)
            {
                case UasSysStatus SysStat:
                    UpdateBatt(SysStat.BatteryRemaining);
                    UpdateSignal(SysStat.DropRateComm);

                    (App.Current as App).syssavefile.WriteLine(
                        $"{(App.Current as App).decrypt.TotalMilliseconds}," +
                        $"{msgprocess.TotalMilliseconds}," +
                        $"{updateui.TotalMilliseconds}");
                    break;
                case UasGlobalPositionInt Gps:
                    UpdateGPS(Gps.Lat, Gps.Lon);
                    UpdateBearing(Gps.Hdg);

                    (App.Current as App).gpssavefile.WriteLine(
                        $"{(App.Current as App).decrypt.TotalMilliseconds}," +
                        $"{msgprocess.TotalMilliseconds}," +
                        $"{updateui.TotalMilliseconds}");
                    break;
                case UasAttitude Att:
                    UpdateAtt(Att.Yaw, Att.Pitch, Att.Roll);
                    UpdateKestabilanTerbang();

                    (App.Current as App).attsavefile.WriteLine(
                        $"{(App.Current as App).decrypt.TotalMilliseconds}," +
                        $"{msgprocess.TotalMilliseconds}," +
                        $"{updateui.TotalMilliseconds}");
                    break;
                case UasHeartbeat HrtBt:
                    UpdateFlightMode(HrtBt.SystemStatus);

                    (App.Current as App).hrtsavefile.WriteLine(
                        $"{(App.Current as App).decrypt.TotalMilliseconds}," +
                        $"{msgprocess.TotalMilliseconds}," +
                        $"{updateui.TotalMilliseconds}");
                    break;
                default:
                    return;
            }

            Debug.WriteLine($"UI {msg} updated in {updateui.TotalMilliseconds} ms");
        }

        private void UpdateFlightMode(MavState state)
        {
            _flightMode = Enum.GetName(typeof(MavState), state);
            OnPropertyChanged("FlightMode");
        }

        private void UpdateAtt(float yawRad, float pitchRad, float rollRad)
        {
            LastSamples.Add(rollRad * 180.0 / Math.PI);
            if(FlightStopwatch != null)
                LastSamplesTime.Add(FlightStopwatch.Elapsed.TotalMilliseconds);

            if (LastSamples.Count > samples)
            {
                LastSamples.RemoveAt(0);
                //LastSamplesTime.RemoveAt(0);
            }

            IMU_Avionic.UpdateUI(pitchRad, rollRad);

            if(!useCompass)
                MapView.UpdateBearing((float)(((yawRad * 180.0 / Math.PI) + 360.0) % 360));
        }

        private void UpdateKestabilanTerbang()
        {
            if (LastSamples.Count < samples)
                return;

            int sigmaX = 0;
            int sigma_XSquare = 0;

            var sigmaY = LastSamples.Sum();
            var sigmaXY = 0d;

            for (int i = 0; i < samples; i++)
            {
                sigmaX += i + 1;
                sigma_XSquare += (i + 1) * (i + 1);
                sigmaXY += (i + 1) * LastSamples[i];
            }

            var grad = ((samples * sigmaXY) - (sigmaX * sigmaY)) / ((samples * sigma_XSquare) - (sigmaX * sigmaX)) * 2;

            //Debug.WriteLine($"Kestabian : m = {grad}");

            var m = Math.Abs(grad);
            MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (m >= 10)
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
                else if (5 < m && m < 10)
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
            });
        }

        private void UpdateGPS(int lat, int lon)
        {
            //lat = -74107080 + new Random().Next(-200, 200);
            //lon = 1127047190 + new Random().Next(-200, 200);

            if (lat == int.MinValue || lon == int.MinValue)
                return;

            MapView.UpdateGPS(lat, lon);
        }

        private void UpdateBearing(ushort hdg)
        {
            if (hdg == ushort.MaxValue || !useCompass)
                return;

            MapView.UpdateBearing((float)((hdg - 9000 + 36000) % 36000 / 100.0));
        }

        private void UpdateBatt(sbyte batteryRemaining)
        {
            _battPercent = $"batt : {batteryRemaining} %";
            OnPropertyChanged("BattPercent");
        }

        private void UpdateSignal(ushort dropRateComm)
        {
            _signalPercent = $"rssi : {100.0 - (dropRateComm / 100.0):0.00} %";
            OnPropertyChanged("SignalPercent");
        }

        #endregion

        #region Button events

        private void ConnSettings_Confirmed(object sender, ConnectionArgs ConnArgs)
        {
            CurrentConnection = ConnArgs;
            ConnectBtn.IsEnabled = true;
            ConnectBtn.BackgroundColor = Color.Red;
        }

        private void Toggle_ConnSettings(object sender, EventArgs e)
        {
            if (!ConnSettings.IsVisible)
            {
                ConnSettings.ShowPanel();
                ConnectBtn.IsEnabled = false;
                ConnectBtn.BackgroundColor = Color.DarkSlateGray;
            }
            else
            {
                ConnSettings.ClosePanel();
                ConnectBtn.IsEnabled = true;
                ConnectBtn.BackgroundColor = Color.Red;
            }
        }

        private async void Connection_Clicked(object sender, EventArgs e)
        {
            if (!IsConnected)
            {
                var valid = false;

                ConnSettingBtn.IsEnabled = false;
                
                (sender as Button).Text = "Connecting";
                (sender as Button).IsEnabled = false;
                (sender as Button).BackgroundColor = Color.DarkGray;

                ShowLoadingOverlay("Menyambungkan UAV . . .");

                switch (CurrentConnection.ConnType)
                {
                    case ConnectionType.USB:
                        

                        var COM = CurrentConnection.Config["COM"] as string;
                        if (!COM.Contains("COM"))
                        {
                            if (!await DisplayAlert("COM Port tidak valid :", COM, "OK", "Ubah"))
                                ConnSettings.ShowPanel();
                            break;
                        }

                        valid = true;

                        if (!await Task.Run(() => (App.Current as App).ReceiverService.ConnectTo(COM, CurrentConnection.Config["Baudrate"] as string)))
                            break;

                        IsConnected = true;
                        
                        break;
                    case ConnectionType.WIFI:
                        if (await Permissions.RequestAsync<Permissions.NetworkState>() != PermissionStatus.Granted)
                            return;

                        var IP = CurrentConnection.Config["IP"] as string;
                        var Port = CurrentConnection.Config["Port"] as string;
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

                        valid = true;

                        if (!await Task.Run(() => ((App.Current as App).ReceiverService as WIFIService).ConnectTo(IP, PortNum)))
                            break;

                        IsConnected = true;
                        
                        break;
                    default:
                        break;
                }

                if (IsConnected)
                {
                    StartFlightTimer();

                    (sender as Button).Text = "Connected";
                    (sender as Button).BackgroundColor = Color.Green;
                }
                else
                {
                    if(valid)
                        await DisplayAlert("Gagal terhubung ke UAV", "UAV tidak ditemukan.", "OK");

                    (sender as Button).Text = "Disconnected";
                    (sender as Button).BackgroundColor = Color.Red;

                    ConnSettingBtn.IsEnabled = true;
                }
                (sender as Button).IsEnabled = true;
            }
            else
            {
                if (!await DisplayAlert("Anda akan memutuskan koneksi UAV", "Anda menekan tombol Disconnect, apakah anda yakin?", "YA, DISCONNECT", "Batal"))
                    return;

                (sender as Button).Text = "Disconnecting";
                (sender as Button).IsEnabled = false;
                (sender as Button).BackgroundColor = Color.DarkBlue;

                ShowLoadingOverlay("Memutuskan koneksi UAV . . .");

                (App.Current as App).attsavefile.Finish();
                (App.Current as App).gpssavefile.Finish();
                (App.Current as App).syssavefile.Finish();
                (App.Current as App).hrtsavefile.Finish();

                if (!await (App.Current as App).ReceiverService.Disconnect())
                    return;
                if ((App.Current as App).MavLinkTransport != null)
                    (App.Current as App).MavLinkTransport.Dispose();

                IsConnected = false;

                if (IsConnected)
                {
                    (sender as Button).Text = "Connected";
                    (sender as Button).BackgroundColor = Color.Green;
                }
                else
                {
                    StopFlightTimer();

                    (sender as Button).Text = "Disconnected";
                    (sender as Button).BackgroundColor = Color.Red;

                    ConnSettingBtn.IsEnabled = true;
                }
                (sender as Button).IsEnabled = true;
            }

            HideLoadingOverlay();
        }

        private void StartFlightTimer()
        {
            FlightStopwatch = Stopwatch.StartNew();

            FlightTimer = new System.Timers.Timer(1 / 60.0);
            FlightTimer.Elapsed += (object sender, System.Timers.ElapsedEventArgs e) =>
            {
                _flightTime = "t+" + FlightStopwatch.Elapsed.ToString("hh\\:mm\\:ss\\.ff");
                OnPropertyChanged("FlightTime");
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

        #region AES Testing

        private async void Button_Clicked(object sender, EventArgs e)
        {
            ShowLoadingOverlay("Testing AES");

            await Task.Run(() =>
            {
                Thread.CurrentThread.Name = "AES Test Thread";

                (App.Current as App).InitializeAES();

                var timer = Stopwatch.StartNew();

                for (int i = 0; i < 1000000; i++)
                {
                    (App.Current as App).EncryptAndGetCipherBytes((App.Current as App).DummyBuf.ToArray());
                }

                timer.Stop();
                Debug.WriteLine($"1 mil Encryption took {timer.ElapsedMilliseconds} ms / {timer.ElapsedTicks} ticks");

                var dummyCipher = (App.Current as App).EncryptAndGetCipherBytes((App.Current as App).DummyBuf.ToArray());

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

                UpdateGPS(lat, lon);

                UpdateBatt(batt);
            });
        }

    #endregion
    }
}
