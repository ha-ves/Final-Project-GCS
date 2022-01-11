using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TugasAkhir_GCS.Interfaces;
using static Com.Hoho.Android.Usbserial.Util.SerialInputOutputManager;

namespace TugasAkhir_GCS.Droid.Services
{
    public class SerialListener : Java.Lang.Object, IListener
    {
        public event NewDataReceived DataReceived;

        public void OnNewData(byte[] p0)
        {
            if (DataReceived != null) DataReceived(this, p0);
        }

        #region unused

        public IntPtr Handle { get; }

        public int JniIdentityHashCode { get; }

        public JniObjectReference PeerReference { get; }

        public JniPeerMembers JniPeerMembers { get; }

        public JniManagedPeerStates JniManagedPeerState { get; }

        public void Dispose()
        {
        }

        public void Disposed()
        {
        }

        public void DisposeUnlessReferenced()
        {
        }

        public void Finalized()
        {
        }

        public void OnRunError(Java.Lang.Exception p0)
        {
        }

        public void SetJniIdentityHashCode(int value)
        {
        }

        public void SetJniManagedPeerState(JniManagedPeerStates value)
        {
        }

        public void SetPeerReference(JniObjectReference reference)
        {
        }

        public void UnregisterFromRuntime()
        {
        }

        #endregion
    }
}