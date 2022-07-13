using Android.App;
using Android.Content;
using Android.Hardware.Usb;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Com.Hoho.Android.Usbserial.Driver;
using Com.Hoho.Android.Usbserial.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TugasAkhir_GCS.Droid.Services;
using TugasAkhir_GCS.Interfaces;
using Xamarin.Forms;
using static Com.Hoho.Android.Usbserial.Util.SerialInputOutputManager;

[assembly: Dependency(typeof(SerialPortService))]
namespace TugasAkhir_GCS.Droid.Services
{
    class SerialPortService : IReceiverService
    {
        public event NewDataReceived DataReceived;

        UsbManager usbManager;

        Dictionary<string, IUsbSerialPort> SerialPorts;
        IUsbSerialPort ConnectedPort;

        public SerialPortService()
        {
            usbManager = (UsbManager)Android.App.Application.Context.GetSystemService(Context.UsbService);
        }

        public Task<string[]> RefreshSerialPorts(CancellationToken cancelToken)
        {
            if (SerialPorts == null)
                SerialPorts = new Dictionary<string, IUsbSerialPort>();
            
            SerialPorts.Clear();

            // Find all available drivers from attached devices
            var availableDrivers = UsbSerialProber.DefaultProber.FindAllDrivers(usbManager);

            var ports = new List<string>();
            foreach (var driver in availableDrivers)
            {
                SerialPorts.Add(driver.Device.ProductName, driver.Ports.FirstOrDefault());

                System.Diagnostics.Debug.WriteLine($"{driver.Device.ProductName}");
                ports.Add($"{driver.Device.ProductName}");
            }

            return Task.FromResult(ports.ToArray());
        }

        public Task<bool> ConnectTo(string portName, string baudrate)
        {
            ConnectedPort = SerialPorts[portName];
            var listener = new SerialListener();
            listener.DataReceived += (sender, data, time) =>
            {
                if (DataReceived != null) DataReceived(this, data, time);
            };
            new SerialInputOutputManager(ConnectedPort, listener).Start();

            return Task.FromResult(true);
        }

        public Task<bool> Disconnect()
        {
            if(ConnectedPort != null)
            {
                ConnectedPort.Close();
                ConnectedPort.Dispose();
            }

            if(SerialPorts != null)
                SerialPorts.Clear();

            if(usbManager != null)
                usbManager.Dispose();

            if (DataReceived != null)
                DataReceived = null;

            return Task.FromResult(false);
        }

        public void SendData(object sender, byte[] buffer)
        {
            
        }
    }
}