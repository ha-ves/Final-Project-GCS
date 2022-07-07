using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TugasAkhir_GCS.CustomView
{
    public partial class AttitudeAvionic : ContentView, INotifyPropertyChanged
    {
        float _rotate = 0;
        public float Rotate { get => _rotate; set { _rotate = value; OnPropertyChanged("Rotate"); } }
        float _transX = 0;
        public float TransX { get => _transX; set { _transX = value; OnPropertyChanged("TransX"); } }
        float _transY = 0;
        public float TransY { get => _transY; set { _transY = value; OnPropertyChanged("TransY"); } }

        public AttitudeAvionic()
        {
            BindingContext = this;
            InitializeComponent();
        }

        public void UpdateUI(float pitchRad, float rollRad)
        {
            var sin = Math.Sin(rollRad);
            var cos = Math.Cos(rollRad);

            var rotate = (float)-(rollRad / Math.PI * 180.0);
            var transX = (float)(pitchRad / Math.PI * 180.0 / 90 * 360 * sin);
            var transY = (float)(pitchRad / Math.PI * 180.0 / 90 * 360 * cos);
#if DATA_FETCH
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Horizon.Rotation = rotate;
                Horizon.TranslationX = transX;
                Horizon.TranslationY = transY;
            });
#else
            new Animation(val =>
            {
                Rotate = (float)val;
            }, start: Rotate, rotate).Commit(this, "RotateAnim", length: App.Current.Resources["AnimLength"] as OnIdiom<byte>);

            new Animation(val =>
            {
                TransX = (float)val;
            }, start: TransX, transX).Commit(this, "TransXAnim", length: App.Current.Resources["AnimLength"] as OnIdiom<byte>);

            new Animation(val =>
            {
                TransY = (float)val;
            }, start: TransY, transY).Commit(this, "TransYAnim", length: App.Current.Resources["AnimLength"] as OnIdiom<byte>);
#endif
        }
    }
}