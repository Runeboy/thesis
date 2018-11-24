using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Airswipe.WinRT.Core.Data
{
    public class JsonSerializable
    {
        #region Methods

        public string ToJson()
        {
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(GetType()).WriteObject(stream, this);
                using (var streamReader = new StreamReader(stream))
                    return streamReader.ReadToEnd();
            }
        }

        #endregion
    }
}
