using Android.App;
using Android.Content;
using Android.Hardware.Usb;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Com.Hoho.Android.Usbserial.Driver;
using Com.Hoho.Android.Usbserial.Util;
using Java.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TugasAkhir_GCS.Droid.Services;
using TugasAkhir_GCS.Interfaces;
using Xamarin.Essentials;
using Xamarin.Forms;
using static Com.Hoho.Android.Usbserial.Util.SerialInputOutputManager;

[assembly: Dependency(typeof(SerialPortService))]
namespace TugasAkhir_GCS.Droid.Services
{
    class SerialPortService : IReceiverService
    {
        public event NewDataReceived DataReceived;

        UsbManager usbManager;

        Dictionary<string, IUsbSerialPort> SerialPorts = new Dictionary<string, IUsbSerialPort>();
        IUsbSerialPort ConnectedPort;

        public SerialPortService()
        {
            usbManager = (UsbManager)Android.App.Application.Context.GetSystemService(Context.UsbService);
        }

        public Task<string[]> RefreshSerialPorts(CancellationToken cancelToken)
        {
            SerialPorts.Clear();

            // Find all available drivers from attached devices
            var availableDrivers = new UsbSerialProber(UsbSerialProber.DefaultProbeTable).FindAllDrivers(usbManager);

            var ports = new List<string>();
            foreach (var driver in availableDrivers)
            {
                SerialPorts.Add(driver.Device.ProductName, driver.Ports.FirstOrDefault());

                System.Diagnostics.Debug.WriteLine($"{driver.Device.ProductName}");
                ports.Add($"{driver.Device.ProductName}");
            }

            return Task.FromResult(ports.ToArray());
        }

        public async Task<bool> ConnectTo(string portName, string baudrate)
        {
            ConnectedPort = SerialPorts[portName];

            var conn = usbManager.OpenDevice(ConnectedPort.Device);

            ConnectedPort.Open(conn);
            ConnectedPort.SetParameters(int.Parse(baudrate), UsbSerialPort.Databits8, UsbSerialPort.Stopbits1, UsbSerialPort.ParityNone);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(() => SerialRead_Entry());
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            if (await (App.Current as App).InitSystem())
                return true;
            else
                return false;
        }

        private void SerialRead_Entry()
        {
            byte[] data = new byte[4096];

            while (true)
            {
                var len = ConnectedPort.Read(data, 50);

                if (len > 0 && DataReceived != null)
                {
                    //int count = 0;

                    //var str = "Android Serial :" + System.Environment.NewLine + "[";

                    //for (int i = 0; i < len; i++)
                    //{
                    //    str += $" {data[i]:X2} ";
                    //    if (++count > 15)
                    //    {
                    //        str += System.Environment.NewLine;
                    //        count = 0;
                    //    }
                    //}
                    //System.Diagnostics.Debug.WriteLine(str + "]");

                    DataReceived(this, data.Take(len).ToArray(), DateTime.Now);
                }
            }
        }

        public Task<bool> Disconnect()
        {
            if(ConnectedPort != null)
            {
                ConnectedPort.Close();
                ConnectedPort = null;
            }

            if (DataReceived != null)
                DataReceived = null;

            return Task.FromResult(true);
        }

        public void SendData(object sender, byte[] buffer)
        {
            
        }
    }
}