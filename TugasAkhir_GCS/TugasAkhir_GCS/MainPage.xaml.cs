using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace TugasAkhir_GCS
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        string _testString = "Testing 1 2 3 4";
        public string TestString { get { return _testString; } set { _testString = value; OnPropertyChanged("TestString"); } }

        public ImageSource ImgUsed { get { return ImageSource.FromResource("PigeonMobile_Xamarin_Cs.Resources.pigeon.png", typeof(App).Assembly); } }

        public MainPage()
        {
            BindingContext = this;
            InitializeComponent();
        }
    }
}
