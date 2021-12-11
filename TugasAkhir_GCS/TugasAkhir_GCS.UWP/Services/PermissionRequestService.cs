using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TugasAkhir_GCS.Interfaces;
using TugasAkhir_GCS.UWP.Services;
using Windows.Devices.Geolocation;
using Xamarin.Essentials;
using Xamarin.Forms;
using static Xamarin.Essentials.Permissions;

[assembly: Dependency(typeof(PermissionRequestService))]
namespace TugasAkhir_GCS.UWP.Services
{
    class PermissionRequestService : IPermissionRequestService
    {
        /// <summary>
        /// Request access to a list of permissions
        /// </summary>
        /// <param name="permissions">The list of permissions to request</param>
        /// <returns>The permissions that are allowed</returns>
        public async void RequestManualPermission(BasePermission permission)
        {
            switch (permission)
            {
                case LocationWhenInUse location:
                    await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-location"));
                    break;
            }
        }
    }
}
