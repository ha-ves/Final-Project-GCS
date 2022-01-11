using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace TugasAkhir_GCS
{
    public static class WithThread
    {
        public static string GetString(string append)
        {
            return $"[{Thread.CurrentThread.Name}] : {append}";
        }
    }
}
