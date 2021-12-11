using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using static Xamarin.Essentials.Permissions;

namespace TugasAkhir_GCS.Interfaces
{
    public interface IPermissionRequestService
    {
        void RequestManualPermission(BasePermission permission);
    }
}
