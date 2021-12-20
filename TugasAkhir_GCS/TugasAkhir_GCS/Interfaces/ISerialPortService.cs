using System;
using System.Collections.Generic;
using System.Text;

namespace TugasAkhir_GCS.Interfaces
{
    public delegate void SerialDataReceived(object serialPort, byte[] data);

    public interface ISerialPortService
    {
        event SerialDataReceived DataReceived;

        void ListSerialPorts();
    }
}
