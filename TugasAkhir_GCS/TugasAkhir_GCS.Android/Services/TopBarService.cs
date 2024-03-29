﻿using Android.App;
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
using Xamarin.Forms;

[assembly: Dependency(typeof(TopBarService))]
namespace TugasAkhir_GCS.Droid.Services
{
    class TopBarService : ITopBarService
    {
        public void PrepareTopBar(params object[] element)
        {
            Grid.SetColumnSpan(element[0] as Frame, 1);
        }
    }
}