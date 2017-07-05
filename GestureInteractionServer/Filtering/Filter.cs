using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestureInteractionServer.Filtering
{
    abstract public class Filter
    {

        //parametri di inizializzazione per il funzionamento del filtro
        protected Dictionary<string, object> parameters;
       

        public abstract void init(Dictionary<string, object> parameters);
        public abstract Array filter(Array point_to_filter);
    }
}
