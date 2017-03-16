using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Managers.DevicesDriver.Kinect1DriverSupport
{
    public class InfoData : AbstractInfo
    {
        public int DisplayWidth { get; set; }
        public int DisplayHeight { get; set; }

        public int SDKVersion { get; set; }

        public InfoData(int width, int height, int version)
        {
            this.DisplayWidth = width;
            this.DisplayHeight = height;

            this.SDKVersion = version;

            this.DataType_ = AbstractInfo.DataType.InfoData;
        }
    }
}
