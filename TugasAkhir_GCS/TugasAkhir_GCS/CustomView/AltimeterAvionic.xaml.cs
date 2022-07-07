using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using TugasAkhir_GCS;
using System.Diagnostics;
using System.ComponentModel;
using Xamarin.Essentials;

namespace TugasAkhir_GCS.CustomView
{
    public partial class AltimeterAvionic : ContentView, INotifyPropertyChanged
    {
        float _rotation = 0;
        public float Rotate { get => _rotation; set { _rotation = value; OnPropertyChanged("Rotate"); } }

        public AltimeterAvionic()
        {
            BindingContext = this;
            InitializeComponent();
        }

        public void UpdateUI(int altitude)
        {
            var rot = ((float)altitude).Map(0, 45000.0f, 0, 270.0f);

            if (rot < 0) rot = 0;
            else if (rot > 270.0f) rot = 270.0f;
#if DATA_FETCH
            MainThread.BeginInvokeOnMainThread(() => alti_needle.Rotation = rot);
#else
            new Animation(val =>
            {
                Rotate = (float)val;
            }, start: Rotate, end: rot).Commit(this, "TransYAnim", length: App.Current.Resources["AnimLength"] as OnIdiom<byte>);
#endif
        }
    }
}