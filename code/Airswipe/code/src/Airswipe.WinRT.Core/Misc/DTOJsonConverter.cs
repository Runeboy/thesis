using Airswipe.WinRT.Core.Data;
using Airswipe.WinRT.Core.Data.Dto;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Airswipe.WinRT.Core.Misc
{
    public class DTOJsonConverter : Newtonsoft.Json.JsonConverter
    {
        private static readonly Dictionary<Type, Type> mappings  =  new Dictionary<Type, Type> {
            { typeof(SpatialPoint), typeof(XYZPoint) },
            { typeof(Rectangle<SpatialPoint>), typeof(XYZRect) },
            { typeof(Rectangle<PlanePoint>), typeof(XYRect) },
            { typeof(SpatialRectangle), typeof(XYZRect) },
            { typeof(SpatialPlane), typeof(XYZPlane) },
            //{ typeof(ProjectedPlanePoint<XYZPoint>), typeof(ProjectedXYPoint) },
            { typeof(PlanePoint), typeof(XYPoint) },
        };

        public override bool CanConvert(Type objectType)
        {
            return mappings.Keys.Contains(objectType);
        }

        public override object ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            return serializer.Deserialize(reader, getMappedType(objectType));
        }

        private Type getMappedType(Type objectType)
        {
            if (!mappings.Keys.Contains(objectType))
                throw new NotSupportedException(string.Format("Type {0} unexpected.", objectType));

            return mappings[objectType];
        }

        public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}