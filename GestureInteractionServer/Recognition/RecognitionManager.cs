using System;
using Recognition.ClStrategies;
using System.Collections.Generic;
using System.Linq;
using Managers;
using GestureInteractionServer.Properties;
using MongoDB.Driver;
using Models;
using System.Diagnostics;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using Accord.Statistics.Models.Markov;
using Accord.Statistics.Distributions.Multivariate;
using Managers.Busses;
using Managers.DevicesDriver;
using System.Net.WebSockets;
using Utilities.Logger;
using System.Threading;
using Utilities;

namespace Recognition
{
    public class RecognitionManager
    {

        public List<Recognizer> recognizers;
        private static RecognitionManager instance;

        public static RecognitionManager Instance
        {
            get
            {

                if (instance == null)
                {
                    instance = new RecognitionManager();
                }
                return instance;
            }
        }

        private RecognitionManager()
        {
            recognizers = new List<Recognizer>();
        }


        public double trainModel(Tuple<string, object[]> x, string dev_id, string strtype, double probthreshold)
        {
             
            //aggiunge un riconoscitore alla lista di quelli esistenti
            BsonValue[] vals = DBManager.Instance.queryDeviceId(dev_id);
            double d;
            if (vals[0] != "")
            {
                Tuple<double[][][], string[]> trainset = DBManager.Instance.retrieve3DGestureSet(dev_id, strtype);
                Recognizer r = new Recognizer((ClassifierStrategy)Activator.CreateInstance(TypeSolver.getTypeByName(x.Item1)[0], dev_id, strtype,  x.Item2), probthreshold);
                d = r.trainStrategy(trainset.Item1, trainset.Item2);
                recognizers.Add(r);
                return d;
            }
            else
                return -1;
            
            
        }


        public bool startTrackDeviceStream(DeviceDriver d, string stream)
        {
           
            Recognizer r=null;
            for (int i = 0; i < recognizers.Count; i++)
            {
                if (recognizers[i].strategy.dev_id == d.identifier && recognizers[i].strategy.strtype == stream)
                {
                    r = recognizers[i];
                    break;
                }
            }

            if (r == null)
            {
                return false;
            }
            else
            {
                r.attachInputBus(d);
                return r.startTracking();  
                
            }
        }

        public bool stopTrackDeviceStream(DeviceDriver d, string stream)
        {
           
            Recognizer r = null;
            for (int i = 0; i < recognizers.Count; i++)
            {
                if (recognizers[i].strategy.dev_id == d.identifier && recognizers[i].strategy.strtype == stream)
                {
                    r = recognizers[i];
                    break;
                }
            }

            if (r == null)
            {
                return false;
            }
            else
            {
                return r.stopTracking();

            }
        }

     
       

        public bool SaveRecognizer(DeviceDriver d, string strtype)
        {
            for (int i = 0; i < recognizers.Count; i++)
            {
                Recognizer r = recognizers[i];
                if(r.strategy.dev_id==d.identifier && r.strategy.strtype == strtype)
                {
                    DBManager.Instance.insertStrategy(r.strategy);
                }
            }
            return true;
        }

        //threshold per la sogliatura riconoscimento del gesto
        //La tupla contiene: id dev, stream type (stringa non id), e bus è il bus di output
        //il metodo associa ai bus outputs una strategia
        public bool init(List<Tuple<string,string,Bus>> outputs, double probthresh)
        {
            
            List<ClassifierStrategy> cl=DBManager.Instance.retrieveStrategies();
            if (cl.Count != outputs.Count)
            {
                Logging.Instance.Error(this.GetType(),"Errore numero di bus di output:{0} e numero riconoscitori:{1} non coerenti",outputs.Count,cl.Count);
                return false;
            }
            for (int i = 0; i < cl.Count; i++)
            {
                Recognizer r = new Recognizer(cl[i], probthresh);
                for (int k = 0; k < outputs.Count; k++)
                {
                    if (outputs[k].Item1 == cl[i].dev_id && outputs[k].Item2 == cl[i].strtype)
                    {
                        r.attachOutputBus(outputs[k].Item3);
                        outputs.RemoveAt(k);
                        recognizers.Add(r);
                        break;
                    }
                } 
            }
            
            return true;
        }





        /*
        public static void Main(string[] args)
        {




            RecognitionManager rf = new RecognitionManager();
            //to be removed
            rf.startmongodb();
            //to be removed
            rf.startDBManager();
            //to be removed
            Dictionary<string, string[]> s = new Dictionary<string, string[]>();
            s["SKELETON"] = new string[] { "WebSocketBus", "127.0.0.1", "10000" };
            Console.WriteLine("Errori:"+ rf.trainModel(new Tuple<string, object[]>("HMMStrategy", new Object[] { 16, 0, 0.1, false, 1e-4, true }), "3", "SKELETON", 0.7));
            //to be removed
            /*DummyDriver d = new DummyDriver("3", "pippo", "pluto", "paperino", s);
            d.start(Settings.Default.DEVICE_STATE_EXCLUSIVE);
            d.enableWriteOnBus(Settings.Default.DEV_OUTTYPE_SKELETON);
            //rf.SaveRecognizer(d, Settings.Default.DEV_OUTTYPE_SKELETON);
           
            WebSocketBus ws = new WebSocketBus("127.0.0.1", "11000");
            rf.init(new List<Tuple<string, string, Bus>> { new Tuple<string, string, Bus>( "3", Settings.Default.DEV_OUTTYPE_SKELETON, ws ) }, 0.8);
            rf.startTrackDeviceStream(d, Settings.Default.DEV_OUTTYPE_SKELETON);
            Thread.Sleep(5000);
            rf.stopTrackDeviceStream(d, Settings.Default.DEV_OUTTYPE_SKELETON);
            Console.ReadKey();
        }
        public void startDBManager()
        {
            string dbname = "test";
            string dbip = "127.0.0.1";
            string dbport = "27017";
            DBManager.Instance.Init(dbname, dbip, Int32.Parse(dbport));
        }
        public void startmongodb()
        {
            string procpath = "C:\\Program Files\\MongoDB\\Server\\3.2\\bin\\mongod";
            string procpars = "--dbpath C:\\mongodata --nojournal --httpinterface --rest";
            Process.Start(new ProcessStartInfo(procpath, procpars));
        }
     */
    }
 
}
