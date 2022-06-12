using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TugasAkhir_GCS.Interfaces;
using TugasAkhir_GCS.UWP.Services;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Xamarin.Forms;

[assembly: Dependency(typeof(FileHandler))]
namespace TugasAkhir_GCS.UWP.Services
{
    public class FileHandler : IFileHandler
    {
        DataWriter savefile;
        IRandomAccessStream filestream;

        public async void Initialize(string filepath)
        {
            var filesave = new FileSavePicker();
            filesave.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            filesave.SuggestedFileName = filepath;
            filesave.FileTypeChoices.Add("CSV", new List<string>() { ".csv" });

            filestream = await (await filesave.PickSaveFileAsync()).OpenAsync(FileAccessMode.ReadWrite);
            savefile = new DataWriter(filestream.GetOutputStreamAt(0));
        }

        public void WriteLine(string line)
        {
            savefile.WriteString(line + Environment.NewLine);
        }

        public async void Finish()
        {
            if(savefile != null)
                await savefile.StoreAsync();
            if (filestream != null)
                await filestream.FlushAsync();

            if (savefile != null)
                savefile.Dispose();
            if (filestream != null)
                filestream.Dispose();

            savefile = null;
            filestream = null;

            Debug.WriteLine("Save File Finished");
        }

    }
}
