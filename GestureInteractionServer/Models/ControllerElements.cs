using System;
using System.Collections.Generic;

namespace Models
{





    public class BusInfo
    {
        public bool BusStatus { get; set; }
        public string StreamName { get; set; }
        public Uri StreamUri { get; set; }
    }


    public class DeviceInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Model { get; set; }

        //Tkey è l'id dello stream, Tvalue è l'uri associato alla stream
        public List<string> ListStreamsName { get; set; }


        public DeviceInfo(string id, string name, string model, List<string> listStreamsName)
        {
            this.Id = id;
            this.Name = name;
            this.Model = model;
            this.ListStreamsName = listStreamsName;
        }

    }
}