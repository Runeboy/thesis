using Newtonsoft.Json;
using System.Collections.Generic;

namespace Airswipe.WinRT.Core.Data
{
    public interface PointComponents
    {
        [JsonIgnore]
        IEnumerable<double> Components { get; set; }

        [JsonIgnore]
        double Length { get; set; }
    }
}
