using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace TugasAkhir_GCS.Droid
{
    [Application]
    [MetaData("com.google.android.geo.API_KEY",
        Value = Variables.GOOGLE_MAPS_ANDROID_API_KEY)]
    class AndroidApp : Application
    {
        public AndroidApp(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }
    }
}