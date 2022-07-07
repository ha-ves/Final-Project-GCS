using System;
using System.Collections.Generic;
using System.Text;

namespace TugasAkhir_GCS
{
    public class Variables
    {
        public const string BING_MAPS_API_KEY = "BRRmmDJXeAGpWegvrwR2~3a5atSGXNkwaG8hkfASdtA~AtSc-tpKsW1ZYz5EdTReaV25tffE0sguBQHOR76pUJ5nXSzpBZ14Y5Z3Bm7HgRcO";

        // dummy mavlink 2.0 attitude msg
        public static List<byte> DummyBuf = new List<byte>()
        { 0xfd, 0x10, 0x00, 0x00, 0x3a, 0x00, 0xc8, 0x1e, 0x00, 0x00, 0xbd, 0x12, 0x01, 0x00, 0x83, 0xa1,
            0xe9, 0xbb, 0xb4, 0x2d, 0x4b, 0xbc, 0x37, 0xd0, 0xaa, 0x3f, 0x0e, 0xa6 };
    }
}
