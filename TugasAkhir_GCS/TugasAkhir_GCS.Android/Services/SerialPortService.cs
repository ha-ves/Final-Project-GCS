using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TugasAkhir_GCS.Droid.Services;
using TugasAkhir_GCS.Interfaces;
using Xamarin.Forms;

[assembly: Dependency(typeof(SerialPortService))]
namespace TugasAkhir_GCS.Droid.Services
{
    class SerialPortService : ISerialPortService
    {
        public event SerialDataReceived DataReceived;

        public void ListSerialPorts()
        {
            
        }
    }
}