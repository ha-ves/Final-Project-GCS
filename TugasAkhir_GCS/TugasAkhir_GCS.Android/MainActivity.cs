using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using Xamarin.Forms.GoogleMaps.Android;
using Android;

namespace TugasAkhir_GCS.Droid
{
    [Activity(
        Theme = "@style/MainTheme",
        ConfigurationChanges =
            ConfigChanges.ScreenSize |
            ConfigChanges.Orientation |
            ConfigChanges.UiMode |
            ConfigChanges.ScreenLayout |
            ConfigChanges.SmallestScreenSize,
        ScreenOrientation = ScreenOrientation.UserLandscape
    )]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        const int RequestLocationId = 0;

        readonly string[] LocationPermissions =
        {
            Manifest.Permission.AccessCoarseLocation,
            Manifest.Permission.AccessFineLocation
        };

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            var platformConfig = new PlatformConfig
            {
                BitmapDescriptorFactory = new CachingNativeBitmapDescriptorFactory()
            };

            Xamarin.FormsGoogleMaps.Init(this, savedInstanceState, platformConfig); // initialize for Xamarin.Forms.GoogleMaps
            
            LoadApplication(new App());
        }

        protected override void OnStart()
        {
            base.OnStart();

            //if ((int)Build.VERSION.SdkInt >= 23)
            //{
            //    if (CheckSelfPermission(Manifest.Permission.AccessFineLocation) != Permission.Granted)
            //    {
            //        RequestPermissions(LocationPermissions, RequestLocationId);
            //    }
            //    else
            //    {
            //        // Permissions already granted - display a message.
            //    }
            //}
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if (requestCode == RequestLocationId)
            {
                if ((grantResults.Length == 1) && (grantResults[0] == (int)Permission.Granted))
                {
                    Console.WriteLine("Location permissions granted.");
                }
                else
                {
                    Console.WriteLine("Location permissions denied.");
                }
            }
            else
            {
                base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            }
        }
    }
}