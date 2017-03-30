using Models;
using System.Collections.Generic;
using System.Web.Http;
using System;
using Managers;
using Managers.DevicesDriver;
using System.Linq;
using GestureInteractionServer.Properties;
using Newtonsoft.Json;
using System.Web.Http.Cors;
using System.Web.Http.Filters;
using Recognition;
using Utilities.Logger;

namespace Controllers
{

    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class DevicesController : ApiController
    {
        // GET: /devices
        public IHttpActionResult Get()
        {
            List<DeviceInfo> outp = new List<DeviceInfo>();
            List<DeviceDriver> devs = DevicesManager.instance.getDevices();

            Logging.Instance.Information(this.GetType(), "Received cmd: /devices");
            try
            {
                for (int i = 0; i < devs.Count; i++)
                {
                    DeviceDriver dev = devs[i];
                    Dictionary<string, string[]> busses = dev.databusparams;
                    List<string> supportedStreams = new List<string>();

                    foreach (string streamName in busses.Keys)
                    {
                        supportedStreams.Add(streamName);
                        Logging.Instance.Information(this.GetType(), streamName);
                    }
                    
                    outp.Add(new DeviceInfo(dev.identifier, dev.name, dev.model, supportedStreams));
                }
               
                return Ok(outp);
            }
            catch(Exception e)
            {
                Logging.Instance.Error(this.GetType(), "Error: {0}" + e);
                return BadRequest(e.Message);
            }
           
        }

        // GET: /devices/start/?id=1&streamsName=SKELETON,RGB&mode=SHARED
        [HttpGet]
        public IHttpActionResult Start(string id, string streamsName, string mode)
        {
            Logging.Instance.Information(this.GetType(), "Received cmd: /devices/start/?id={0}&streams={1}&mode={2}", id, streamsName, mode);

            DeviceDriver dev = DevicesManager.instance.getDevice(id);
            if (dev == null)
                return NotFound();

            try
            {
                List<string> outTypes = streamsName.Split(',').ToList<string>();
                List<BusInfo> busses = new List<BusInfo>();

                bool started = DevicesManager.Instance.startDevice(id, mode, outTypes);
                

                dev = DevicesManager.instance.getDevice(id);
                if (started)
                {
                    for (int i = 0; i < outTypes.Count; i++)
                    {
                        //controllo lo stato dei singoli bus
                        BusInfo busInfo = new BusInfo();

                        //converto tutto in maiuscole
                        busInfo.StreamName = outTypes[i].ToUpper();
                        

                        bool busActive = dev.bus.ContainsKey(busInfo.StreamName);
                        busInfo.BusStatus = busActive ? true : false;

                        //rendere più elegante
                        if (busActive)
                            busInfo.StreamUri = new Uri(dev.bus[busInfo.StreamName].uri);
                        else
                            busInfo.StreamUri = null;

                        busses.Add(busInfo);
                    }
                    
                    return Ok(busses);
                }
                else
                {
                    string response = ("ERROR on starting device " + id);
                    return BadRequest(response);
                }
            }
            catch
            {
                return NotFound();
            }
        }

        // GET: /devices/stop/?id=1
        [HttpGet]
        public IHttpActionResult Stop(string id)
        {
            Logging.Instance.Information(this.GetType(), "Received cmd: /devices/stop/?id={0}", id);
            DeviceDriver dev = DevicesManager.instance.getDevice(id);
            if (dev == null)
                return NotFound();

            if (DevicesManager.Instance.stopDevice(id))
                return Ok("ALL BUS CLOSED");
            else
            {
                string response = ("ERROR on stopping device with id = " + id);
                return BadRequest(response);
            }
        }
    }
}