using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TugasAkhir_GCS.CustomView
{
    public partial class AttitudeAvionic : ContentView, INotifyPropertyChanged
    {
        float _rotate = 0;
        public float Rotate { get => _rotate; }
        float _transX = 0;
        public float TransX { get => _transX; }
        float _transY = 0;
        public float TransY { get => _transY; }

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

            _rotate = rotate;
            OnPropertyChanged("Rotate");

            _transX = transX;
            OnPropertyChanged("TransX");

            _transY = transY;
            OnPropertyChanged("TransY");

            //new Animation(val =>
            //{
            //    _rotate = (float)val;
            //    OnPropertyChanged("Rotate");
            //}, Rotate, rotate).Commit(this, "RotateAnim", rate: 5, length: 30);

            //new Animation(val =>
            //{
            //    _transX = (float)val;
            //    OnPropertyChanged("TransX");
            //}, TransX, transX).Commit(this, "TransXAnim", rate: 5, length: 30);

            //new Animation(val =>
            //{
            //    _transY = (float)val;
            //    OnPropertyChanged("TransY");
            //}, TransY, transY).Commit(this, "TransYAnim", rate: 5, length: 30);
        }
    }
}