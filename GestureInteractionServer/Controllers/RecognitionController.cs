using System;
using System.Web.Http;
using System.Web.Http.Cors;
using Recognition;
using Models;
using Utilities.Logger;
using Managers.DevicesDriver;
using Managers;

namespace Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class RecognitionController : ApiController
    {

        public IHttpActionResult Get(string devId, string streamName)
        {
            return Ok();
        }

        [HttpGet]
        public IHttpActionResult Start(string devId, string streamName)
        {
            Logging.Instance.Information(this.GetType(), "Received cmd: /recognition/start/?devId={0}&streamName={1}", devId, streamName);

            DeviceDriver dev = DevicesManager.instance.getDevice(devId);
            if (RecognitionManager.Instance.startTrackDeviceStream(dev, streamName))
            {
                BusInfo busInfo = new BusInfo();
                busInfo.StreamName = streamName;
                busInfo.BusStatus = dev.bus.ContainsKey(busInfo.StreamName);
                busInfo.StreamUri = new Uri(dev.bus[busInfo.StreamName].uri);

                return Ok(busInfo);
            }
             else
                return BadRequest("Unable to start the recognition!");
        }

        [HttpGet]
        public IHttpActionResult Stop(string devId, string streamName)
        {
            Logging.Instance.Information(this.GetType(), "Received cmd: /recognition/stop/?devId={0}&streamName={1}", devId, streamName);

            DeviceDriver dev = DevicesManager.instance.getDevice(devId);
            bool output = RecognitionManager.Instance.stopTrackDeviceStream(dev, streamName);

            return Ok(output);
        }

    }

}