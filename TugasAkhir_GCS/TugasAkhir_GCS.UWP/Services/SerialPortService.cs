using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TugasAkhir_GCS.Interfaces;
using TugasAkhir_GCS.UWP.Services;
using Windows.Devices.Enumeration;
using Xamarin.Forms;

[assembly: Dependency(typeof(SerialPortService))]
namespace TugasAkhir_GCS.UWP.Services
{
    class SerialPortService : ISerialPortService
    {
        public event SerialDataReceived DataReceived;

        public void ListSerialPorts()
        {
            
        }
    }
}
