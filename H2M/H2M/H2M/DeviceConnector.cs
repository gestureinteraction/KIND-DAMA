using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Net;
using WebSocketSharp.Net;
using WebSocketSharp;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows.Forms;


namespace H2M
{

   
    class DeviceConnector
    {
        [DllImport("user32.dll")]
        public static extern int SetCursorPos(int x, int y);

        public bool device_connected;
        public string devicesstartcall;
        public string devicesstartanswer;
        private Form1 parent;
        private WebSocket ws;
        float screenmaxX = Screen.PrimaryScreen.Bounds.Width;
        float screenmaxY = Screen.PrimaryScreen.Bounds.Height;

        float LeapboundingVolumemaxX = 200;
        float LeapboundingVolumemaxZ = 100;

        public DeviceConnector(Form1 invoker)
        {
            devicesstartcall = "http://127.0.0.1:8080/devices/start/?id=6&streamsName=SKELETON&mode=SHARED";
            devicesstartanswer = "default";
            device_connected = false;
            parent = invoker;
            Thread.Sleep(5000);
            tryConnectionToDevice();
        }

        public void tryConnectionToDevice()
        {

            string result = get(devicesstartcall);
           
            
            try
            {
                JArray json = (JArray)JsonConvert.DeserializeObject(result);
                devicesstartanswer = (string) json[0]["StreamUri"];
                ws=new WebSocket(devicesstartanswer);
                ws.Connect();
                ws.OnMessage += (sender, e) => {//manage data

                    
                    JObject joints = (JObject)JsonConvert.DeserializeObject(e.Data);
                    JArray singlej = (JArray) joints["joints"];
                    JObject coordinates = (JObject)singlej[1]["Pos3d"];
                    float x = (float)coordinates["x"];
                    float z = (float)coordinates["z"];

                    float screenX = (screenmaxX*x/LeapboundingVolumemaxX+screenmaxX)*0.5f;
                    float screenY = (screenmaxY * z / LeapboundingVolumemaxZ + screenmaxY) * 0.5f;

                    VirtualMouse.MoveTo(screenX, screenY, screenmaxX, screenmaxY);
                    //control using VirtualMouse
                    //
                };
                ws.OnClose += (sender, e) =>
                  {
                      device_connected = false;
                      parent.Refresh();
                  };
                if (ws.Ping())
                    device_connected= true;
                else
                   device_connected = false;
               
            }
            catch (Exception e)
            {
                devicesstartanswer = e.ToString();
            }

            parent.Refresh();
            

        }



        private string get(string url)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                string responseFromServer;

                using (var stream = request.GetResponse().GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    responseFromServer=reader.ReadToEnd();
                }

                return responseFromServer;
            }

            catch (Exception ex)
            {
                return ex.ToString();
            }
        }
       
    }
}
