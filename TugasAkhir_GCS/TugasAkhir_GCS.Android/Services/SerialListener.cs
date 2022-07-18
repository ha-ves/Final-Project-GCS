using Java.Interop;
using System;
using System.Collections.Generic;
using System.Text;
using TugasAkhir_GCS.Interfaces;
using static Com.Hoho.Android.Usbserial.Util.SerialInputOutputManager;

namespace TugasAkhir_GCS.Droid.Services
{
    internal class SerialListener : Java.Lang.Object, IListener
    {
        public event EventHandler<byte[]> NewData;
        public void OnNewData(byte[] p0)
        {
            if (NewData != null)
                NewData(this, p0);
        }

        public void OnRunError(Java.Lang.Exception p0)
        {
            System.Diagnostics.Debug.WriteLine(p0);
        }
    }
}
