using System;
using System.Collections.Generic;
using System.Text;

namespace TugasAkhir_GCS
{
    internal static class Extensions
    {
        internal static float Map(this float value, float from_low, float from_high, float to_low, float to_high)
        {
            return to_low + (value - from_low) * (to_high - to_low) / (from_high - from_low);
        }

        internal static double Map(this double value, double from_low, double from_high, double to_low, double to_high)
        {
            return to_low + (value - from_low) * (to_high - to_low) / (from_high - from_low);
        }

        internal static int Map(this int value, int from_low, int from_high, int to_low, int to_high)
        {
            return to_low + (value - from_low) * (to_high - to_low) / (from_high - from_low);
        }
    }
}
