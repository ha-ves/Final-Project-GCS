using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TugasAkhir_GCS.CustomView
{
    public partial class BearingAvionic : ContentView
    {
        float _rotation = 0;
        public float Rotate { get => _rotation; set { _rotation = value; OnPropertyChanged("Rotate"); } }

        public BearingAvionic()
        {
            BindingContext = this;
            InitializeComponent();
        }

        public void UpdateUI(float bearing)
        {
#if DATA_FETCH
            MainThread.BeginInvokeOnMainThread(() => bearing_wheel.Rotation = bearing);
#else
            Rotate = bearing;
#endif
        }
    }
}