using Utilities;
using GestureInteractionServer.Properties;
using Utilities.Logger;
using Managers.Busses;
using Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Managers.DevicesDriver
{
    public class  CursorDriver: DeviceDriver
    {

        public int DisplayWidth { get; set; }
        public int DisplayHeight { get; set; }

        

        public CursorDriver(String id, String name, String model, String producer, Dictionary<string, string[]> pars) : base(id, name, model, producer, pars)
        {
            //properties driver dependent
            databusparams[Settings.Default.DEV_OUTTYPE_CURSOR] = null;
            DisplayWidth = Screen.PrimaryScreen.Bounds.Width;
            DisplayHeight = Screen.PrimaryScreen.Bounds.Height;
            //riempio i parametri dei bus
            fillDataBusParamsKeys(pars);

        }

        public override bool start(String starttype)
        {
            bool notConnected = base.start(starttype);

            if (notConnected)
            {
                //la posizione del cursore è sempre disponibile e non genererà mai un errore
                return true;
            }
            else
                return false;

        }

        public override string enableWriteOnBus(string key)
        {

            //se il bus è gia attivo oppure non è gestibile per qualche motivo
            String prova = base.enableWriteOnBus(key);
            if (prova == Settings.Default.BUS_ALREADY_ACTIVATED || prova == Settings.Default.BUS_NOT_MANAGED)
                return prova;



            if (key == Settings.Default.DEV_OUTTYPE_CURSOR)
            {
                outthread[key] = new Thread(pushCursorData);
                //per ora il tipo di bus da  aprire è hardcoded, e non viene gestito
                //in futuro paramlist[0] sarà il tipo di bus da istanziare
                //i restanti parametri sono i parametri di inizializzazione

                //istanziare con reflection
                bus[key] = (Bus)Activator.CreateInstance(TypeSolver.getTypeByName(databusparams[key][0])[0], databusparams[key][1], databusparams[key][2]);
                outthread[key].Start();
                //aggiungere altri tipi di dato maneggiato eventualmente
                return Settings.Default.BUS_ACTIVATED;
            }
            else
                return Settings.Default.BUS_NOT_MANAGED;
        }


        public void pushCursorData()
        {
           
            // Main processing loop
            while (true)
            {
                try
                {

                    Pos2DFrameElement pos=new Pos2DFrameElement(Cursor.Position.X, Cursor.Position.Y,DateTime.UtcNow.Ticks);
                    bus[Settings.Default.DEV_OUTTYPE_CURSOR].receive(this, Settings.Default.DEV_OUTTYPE_CURSOR, JsonConvert.SerializeObject(pos));
                }
                    
                catch (Exception e)
                {
                    Logging.Instance.Information(this.GetType(), "Errore processamento posizione cursore:" + e.ToString());
                }

                //ottiene 40 posizioni al secondo
                Thread.Sleep(25);

            }


        }

        public override void Dispose()
        {
            // Clean up
            Logging.Instance.Information(this.GetType(), "Cursor device connection closed.");
        }
    }



    

}
