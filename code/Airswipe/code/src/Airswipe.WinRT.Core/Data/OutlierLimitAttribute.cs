using System;

namespace Airswipe.WinRT.Core.Data
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class OutlierLimitAttribute : Attribute
    {
        public OutlierLimitAttribute(double limit)
        {
            Limit = limit;
        }

        public double Limit { get; private set; }
    }
}
