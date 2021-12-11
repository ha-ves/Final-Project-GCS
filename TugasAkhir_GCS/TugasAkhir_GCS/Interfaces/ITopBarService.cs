using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace TugasAkhir_GCS.Interfaces
{
    public interface ITopBarService
    {
        void PrepareTopBar(params object[] element);
    }
}
