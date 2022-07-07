using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TugasAkhir_GCS.Interfaces
{
    public interface IFileHandler
    {
        void Initialize(string filepath);

        void Write(string str);

        void WriteLine(string line);

        void Finish();
    }
}
