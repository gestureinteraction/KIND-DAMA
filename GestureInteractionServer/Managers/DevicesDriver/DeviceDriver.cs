using Managers.Busses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Utilities.Logger;
using GestureInteractionServer.Properties;

namespace Managers.DevicesDriver
{

    public abstract class DeviceDriver : IDisposable
    {
        //logger per l'invo di informazioni
        protected readonly ILogger _logger;

        //identifica lo stato shared, exclusive, closed (scrivere setter e getter)
        public String dev_state { get; set; }
        //dati del dispositivo
        public String identifier { get; set; }
        public string model { get; set; }
        public string name { get; set; }
        public string producer { get; set; }

        //Associa ad ogni tipo di dato spedito in output, l'oggetto bus correntemente istanziato
        public Dictionary<String, Bus> bus { get; set; }

        //Associa ad ogni tipo di dato spedito in output, l'oggetto thread che invia i dati al bus
        public Dictionary<String, Thread> outthread { get; set; }

        //indica i tipi di dato maneggiato: le chiavi sono inizializzate dal costruttore
        //la chiave contiene il nome del dato di output, statico e gestito internamente
        //il valore è un array che contiene: bus da istanziare tramite reflection (all' indice zero) e i parametri per istanziarlo

        public Dictionary<string, string[]> databusparams;

        //controlla il numero di clients connessi e ferma il dispositivo se non ce ne sono
        Thread clientsverifier;



        public DeviceDriver(String id, String name, String model, String producer, Dictionary<string, string[]> startparams)
        {
            _logger = Logging.Instance;

            //inizializzo un dispositivo con id specifico 
            //assicurarsi che esso sia univoco perchè la "equals" viene calcolata sulla base dell'id
            this.identifier = id;
            this.model = model;
            this.name = name;
            this.producer = producer;

            //dizionario dei bus e dei thread vuoti
            bus = new Dictionary<string, Bus>();
            outthread = new Dictionary<string, Thread>();
            //nessun dato viene gestito dalla classe astratta
            databusparams = new Dictionary<string, string[]>();

            //stato iniziale "not connected"
            dev_state = Settings.Default.DEVICE_STATE_NOT_CONNECTED;



        }

        protected void fillDataBusParamsKeys(Dictionary<string, string[]> pars)
        {
            foreach (var i in pars.Keys)
            {

                if (databusparams.ContainsKey(i))
                {
                    //verifico che il tipo sia istanziabile

                    try
                    {
                        //verifico che il tipo di bus sia istanziabile esista
                        Type.GetType(pars[i][0]);
                        databusparams[i] = pars[i];
                    }
                    catch (Exception e)
                    {
                        _logger.Error(this.GetType(), e);
                    }

                }
            }
        }


        public override bool Equals(object obj)
        {
            //necessario per effettuare ricerche e confronti sulle liste di devices
            DeviceDriver d = (DeviceDriver)obj;
            if (d.identifier == this.identifier)
                return true;
            else
                return false;
        }


        public virtual bool start(String starttype)
        {

            
            //da not_connected passo a shared o exclusive
            if ((dev_state == Settings.Default.DEVICE_STATE_NOT_CONNECTED) && ((starttype == Settings.Default.DEVICE_STATE_SHARED || starttype == Settings.Default.DEVICE_STATE_EXCLUSIVE)))
            {
                
                dev_state = starttype;
                clientsverifier = new Thread(stopIfNoClients);
                clientsverifier.Start();
                //eventualmente nel metodo start del driver concreto si effettua una rollback qualora ci siano errori
                return true;
            }
            else if ((dev_state == Settings.Default.DEVICE_STATE_SHARED) && (starttype == Settings.Default.DEVICE_STATE_SHARED))
            {

                return true;
            }
            else
                //in tutti gli altri casi
                return false;
        }

        public virtual bool stop()
        {
            //elimina bus e threads dal dispositivo e lo resetta a "not connected"
            //i parametri databusparams NON devono essere reinizializzati in quanto gestiti dal costruttore

            string[] chiavi = bus.Keys.ToArray();
            foreach (var key in chiavi)
            {
                destroyBus(key);
            }
            dev_state = Settings.Default.DEVICE_STATE_NOT_CONNECTED;
            clientsverifier.Abort();
            Dispose();
            return true;
        }

        public virtual String enableWriteOnBus(String key)
        {
            //questo metodo implementa la istanziazione di un thread e di un bu specifico
            //per ogni tipo di dato gestito dal dispositivo (es. RGB,DEPTH,JOINT,....)
            //nella classe concreta bisogna implementare un metodo push per ciascun tipo di dato gestito
            //tale metodo invia il tipo di dato specifico verso il bus invocando il metodo "receive" per la scrittura dei dati sul bus

            //se il dispositivo è attivo, se il bus richiesto è gestito, e se il bus non è già stato istanziato
            if (dev_state != Settings.Default.DEVICE_STATE_NOT_CONNECTED && databusparams.ContainsKey(key) && !bus.ContainsKey(key))
            {
                //lo attivo
                return Settings.Default.BUS_ACTIVATED;
            }
            if (bus.ContainsKey(key))
            {
                //il bus è già attivo
                return Settings.Default.BUS_ALREADY_ACTIVATED;
            }

            //in tutti gli altri casi mi spiace ma non posso gestirlo
            return Settings.Default.BUS_NOT_MANAGED;
        }

        public virtual bool destroyBus(String key)
        {
            //se il bus da distruggere è stato istanziato
            if (bus.Keys.Contains(key))
            {
                try
                {
                    //ferma il thread precedentemente istanziato
                    outthread[key].Abort();
                    //rimuove il thread dalla lista di thread abilitati alla scrittura
                    outthread.Remove(key);
                    //distrugge il bus relativo
                    bus[key].Dispose();
                    //lo rimuove da quelli correntemente gestiti
                    bus.Remove(key);

                    return true;
                }
                catch (Exception e)
                {
                    _logger.Error(this.GetType(), "Eccezione rimozione bus: {0}", e);
                    return false;
                }
            }
            return false;
        }

        public virtual void Dispose()
        {

        }


        //daemon which checks the presence of attached clients
        public void stopIfNoClients()
        {
            //20 seconds windows
            Thread.Sleep(20000);
            int totalclients = 0;
            foreach (string i in bus.Keys)
                totalclients += bus[i].CountClients();


            if (totalclients == 0)
                stop();
        }
    }

}