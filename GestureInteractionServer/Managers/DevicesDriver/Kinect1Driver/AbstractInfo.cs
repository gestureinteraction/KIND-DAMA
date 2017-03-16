using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Managers.DevicesDriver.Kinect1DriverSupport
{
    public abstract class AbstractInfo
    {
        public enum DataType
        {
            InfoData,
            SkeletonData
        }

        [JsonProperty(PropertyName = "DataType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public DataType DataType_ { get; set; }
    }
}
