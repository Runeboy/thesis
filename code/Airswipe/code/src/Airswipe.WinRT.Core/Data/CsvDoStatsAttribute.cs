using System;

namespace Airswipe.WinRT.Core.Data
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class CsvDoStatsAttribute : Attribute
    {
        public CsvDoStatsAttribute()
        {
            Std = false;
            Mean = true;
            Confidence = true;
        }

        public bool Std { get; set; }
        public bool Mean { get; set; }
        public bool Confidence { get; set; }
    }
}
