using System;

namespace Airswipe.WinRT.Core.Data
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class CsvModifyAttribute : Attribute
    {
        public CsvModifyAttribute()
        {
            Multiply = 1;
        }

        public double Multiply { get; set; }
    }
}
