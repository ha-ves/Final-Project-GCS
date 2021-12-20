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
using Xamarin.Essentials;
using Xamarin.Forms;

[assembly: Dependency(typeof(PermissionRequestService))]
namespace TugasAkhir_GCS.Droid.Services
{
    class PermissionRequestService : IPermissionRequestService
    {
        public void RequestManualPermission(Permissions.BasePermission permission)
        {
            
        }
    }
}