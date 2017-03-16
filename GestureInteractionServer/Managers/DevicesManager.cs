using System;
using System.Collections.Generic;
using Managers.DevicesDriver;
using Utilities.Logger;


namespace Managers
{
    public class DevicesManager
    {
        // lista dei dispositivi aggiunti
        private List<DeviceDriver> drivers;
        public static DevicesManager instance;
       

        public static DevicesManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DevicesManager();
                }
                return instance;
            }
        }

        private DevicesManager() {
            drivers = new List<DeviceDriver>();
        }

        public DeviceDriver getDevice(string id)
        {
            for (int i = 0; i < drivers.Count; i++)
            {
                if (drivers[i].identifier == id)
                    return drivers[i];
            }
            return null;
        }

        public bool addDevice(DeviceDriver dev) {
            //se il driver e' gia presente (controllo tramite identificativo numerico)
            if (drivers.Contains(dev))
                return false;
            else {
                drivers.Add(dev);
                return true;
            }
        }

        public List<DeviceDriver> getDevices()
        {
            return drivers;
        }

        //id è l'id del dispositivo da attivare
        //starttype e' esclusivo o shared
        //datatypes indica quali tipi di dato si intende ottenere dal dispositivo
        public bool startDevice(String id, String starttype, List<String> datatypes)
        {
            for (int i = 0; i < drivers.Count; i++)
            {
                
                if (drivers[i].identifier == id)
                {
                   //controllo se può partire nella modalità selezionata
                    bool test = drivers[i].start(starttype);
                    
                    if (test)
                    {
                        for (int j = 0; j < datatypes.Count; j++)
                        {
                            //faccio partire i bus richiesti
                            //Gestire quando uno o più bus non possono essere avviati
                            drivers[i].enableWriteOnBus(datatypes[j]);
                        }
                        return true;
                    }
                    //se il dev è occupato ritorno false
                    return false;
                }
            }
            //se il dev non è presente in lista ritorno false
            return false;
        }

        public bool stopDevice(String id) {
            
            for (int i = 0; i < drivers.Count; i++)
            {
                if (drivers[i].identifier == id)
                {
                    drivers[i].stop();
                    return true;
                }
            }
            //se il dev non è presente in lista ritorno false
            return false;
        }
    }
}
