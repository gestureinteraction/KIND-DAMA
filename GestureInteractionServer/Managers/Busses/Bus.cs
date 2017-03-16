using System;
using Managers.DevicesDriver;
using Utilities.Logger;

namespace Managers.Busses
{
    public abstract class Bus
    {
        public string uri;

        public Bus()
        {
            
            
        }

        public virtual void receive(DeviceDriver sender_dev, String data_type, Object content) {
            //receive data from the sender and put them into the output channel
            //data_type e' di tipo "JOINT","RGB","DEPTH",.....
            //effettuare di conseguenza il cast in base al tipo di dato contenuto
            //e spedirlo in output
        }

        public virtual void Dispose()
        {
        }


        //ritorna il numero di clients connessi
        public virtual int CountClients() {
            return 0;
        }
       
    }
}
