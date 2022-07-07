using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TugasAkhir_GCS.Droid.Services;
using TugasAkhir_GCS.Interfaces;
using Xamarin.Essentials;
using Xamarin.Forms;
using Environment = Android.OS.Environment;

[assembly: Dependency(typeof(FileHandler))]
namespace TugasAkhir_GCS.Droid.Services
{
    internal class FileHandler : IFileHandler
    {
        StreamWriter savefile;

        public void Finish()
        {
            savefile.Flush();
            savefile.Dispose();
        }

        public async void Initialize(string filepath)
        {
            var path = Environment.GetExternalStoragePublicDirectory(Environment.DirectoryDocuments).AbsolutePath + '/' + filepath;

            savefile = new StreamWriter(File.Create(path));
            savefile.AutoFlush = false;
        }

        public void Write(string str)
        {
            savefile.Write(str);
        }

        public void WriteLine(string line)
        {
            savefile.WriteLine(line);
        }
    }
}