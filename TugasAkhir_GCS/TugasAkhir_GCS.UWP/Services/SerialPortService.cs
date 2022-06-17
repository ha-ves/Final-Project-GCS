using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TugasAkhir_GCS.Interfaces;
using TugasAkhir_GCS.UWP.Services;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;
using Xamarin.Forms;

using Buffer = Windows.Storage.Streams.Buffer;
using MainApp = TugasAkhir_GCS.App;

[assembly: Dependency(typeof(SerialPortService))]
namespace TugasAkhir_GCS.UWP.Services
{
    class SerialPortService : IReceiverService
    {
        public event NewDataReceived DataReceived;

        Dictionary<string, DeviceInformation> SerialPorts;

        SerialDevice ConnectedPort;

        CancellationTokenSource cancelserial;

        public async Task<string[]> RefreshSerialPorts()
        {
            SerialPorts = new Dictionary<string, DeviceInformation>();
            var ports = new List<string>();

            var devices = await DeviceInformation.FindAllAsync(SerialDevice.GetDeviceSelector());

            foreach (var device in devices)
            {
                using (var serialport = await SerialDevice.FromIdAsync(device.Id))
                {
                    if (serialport != null)
                    {
                        SerialPorts.Add(serialport.PortName, device);
                        ports.Add(serialport.PortName);
                        Debug.WriteLine(WithThread.GetString($"Device found : {serialport.PortName}"));
                    }
                }
            }

            return ports.ToArray();
        }

        public async Task<bool> ConnectTo(string portName, string baudrate)
        {
            ConnectedPort = await SerialDevice.FromIdAsync(SerialPorts[portName].Id);

            if (ConnectedPort != null)
            {
                ConnectedPort.BaudRate = uint.Parse(baudrate);
                ConnectedPort.DataBits = 8;
                ConnectedPort.Parity = SerialParity.None;
                ConnectedPort.StopBits = SerialStopBitCount.One;
                ConnectedPort.Handshake = SerialHandshake.None;
                ConnectedPort.ReadTimeout = TimeSpan.FromMilliseconds(1);
                ConnectedPort.WriteTimeout = TimeSpan.FromMilliseconds(1);
            }
            else return false;

            cancelserial = new CancellationTokenSource();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(() => SerialRead_Entry(cancelserial.Token));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            if (await (MainApp.Current as MainApp).InitializeTransport())
                return true;

            await Disconnect();

            return false;
        }

        public Task<bool> Disconnect()
        {
            if(cancelserial != null)
            {
                if(!cancelserial.IsCancellationRequested)
                    cancelserial.Cancel();
                cancelserial.Dispose();
            }

            if(ConnectedPort != null)
                ConnectedPort.Dispose();

            SerialPorts.Clear();

            DataReceived = null;

            return Task.FromResult(true);
        }

        async void SerialRead_Entry(CancellationToken canceltoken)
        {
            Thread.CurrentThread.Name = "SerialReader Thread";

            Buffer RxBuf = new Buffer(4096);

            while (true)
            {
                try
                {
                    await ConnectedPort.InputStream.ReadAsync(RxBuf, RxBuf.Capacity, InputStreamOptions.Partial).AsTask(canceltoken);
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine("SerialReader read canceled.\r\n");
                    return;
                }
                catch(Exception exc)
                {
                    Debug.WriteLine("SerialReader read Exception.\r\n");
                    if (exc.HResult == unchecked((int)0x800703E3))
                        Debug.WriteLine(WithThread.GetString("Disconnected"));
                    break;
                }

                if (RxBuf.Length > 0)
                {
                    //Debug.WriteLine($"SerialReader received -> {RxBuf.Length}[");
                    var receiveTime = DateTime.Now;

                    int count = 0;

                    foreach (var hex in RxBuf.ToArray())
                    {
                        Debug.Write($" {hex:X2} ");
                        if (++count > 15)
                        {
                            Debug.WriteLine("");
                            count = 0;
                        }
                    }
                    Debug.WriteLine("]");

                    if (DataReceived != null) DataReceived(this, RxBuf.ToArray(), receiveTime);
                }
            }
        }

        public async void SendData(object sender, byte[] buffer)
        {
            if (ConnectedPort == null)
                return;

            await ConnectedPort.OutputStream.WriteAsync(buffer.AsBuffer());
        }
    }
}
