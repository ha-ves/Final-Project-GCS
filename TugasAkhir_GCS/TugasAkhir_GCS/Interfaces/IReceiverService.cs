using MavLinkNet;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TugasAkhir_GCS.Interfaces
{
    public delegate void NewDataReceived(object sender, byte[] data);

    public interface IReceiverService
    {
        /// <summary>
        /// Gets called when new data arrived at the serialport.
        /// </summary>
        event NewDataReceived DataReceived;

        /// <summary>
        /// Lists all available serial ports.
        /// </summary>
        /// <returns><b><i>COM Ports</i></b> string array.
        /// <b><i>empty</i></b> if no ports are found.</returns>
        Task<string[]> RefreshSerialPorts();

        /// <summary>
        /// Connect to the serial port.
        /// </summary>
        /// <param name="portName">port name to connect to</param>
        /// <returns><b><i>true</i></b> if connected. <b><i>false</i></b> if otherwise.</returns>
        Task<bool> ConnectTo(string portName, string baudrate);

        /// <summary>
        /// Disconnect from currently connected port.
        /// </summary>
        /// <returns><b><i>true</i></b> if succeed. <b><i>false</i></b> if otherwise.</returns>
        Task<bool> Disconnect();

        /// <summary>
        /// Send data to the connected serial port
        /// </summary>
        /// <param name="sender">class that sends the data</param>
        /// <param name="buffer">the data buffer to send</param>
        /// <returns></returns>
        void SendData(object sender, byte[] buffer);
    }
}
