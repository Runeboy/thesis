using System;

namespace Airswipe.WinRT.Core.Data
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class CsvIgnoreAttribute : Attribute
    {
        //public CsvIgnoreAttribute();
    }
}
