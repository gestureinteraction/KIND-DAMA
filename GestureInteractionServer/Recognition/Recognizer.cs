using Utilities.Logger;
using Managers.Busses;
using Managers.DevicesDriver;
using Recognition.ClStrategies;
using System;
using System.Collections.Generic;
using System.Threading;
using WebSocketSharp;
using Newtonsoft.Json;
using Models;
using GestureInteractionServer.Properties;
using System.Linq;

namespace Recognition
{
    public class Recognizer
    {

        //strategia associata al riconoscitore
        public ClassifierStrategy strategy;
        //soglia con la quale vengono riconosciuti i gesti
        public double probthreshold;
        //connessione generica al dispositivo
        WebSocket connection;
        //da dove leggo i dati
        Bus input;
        //dove scrivo i dati dei gesti
        Bus output;
        //controlla il numero di clients connessi e ferma il riconoscitore se non ce ne sono
        Thread clientsverifier;

        //dati in finestra per il riconoscimento
        List<double[]> windoweddata;

        //vettore delle posizioni x,y,z del frame corrente
        double[] currentframe;

        //conta quanti frame risultano "fermi"
        int stopcounter;

        //stato che indica se il buffer sta accumulando frames o no. se sto fermo non accumulo nulla
        bool state;


        public Recognizer(ClassifierStrategy strategy, double probthreshold)
        {
            this.strategy = strategy;
            this.probthreshold = probthreshold;
            input = null;
            output = null;

            currentframe = null;
            windoweddata = new List<double[]>();
            state = false;
            stopcounter = 0;
        }

        public bool attachInputBus(DeviceDriver d)
        {
            if (strategy.dev_id != d.identifier) {
                Logging.Instance.Error(this.GetType(), "Strategy not suitable for this device");
                return false;
            }
            if (!d.bus.ContainsKey(strategy.strtype)) {
                Logging.Instance.Error(this.GetType(), "Strategy not suitable for this device");
                return false;
            }
            if (d.bus[strategy.strtype] == null)
            {
                Logging.Instance.Error(this.GetType(), "Bus not started for output");
                return false;
            }
            this.input = d.bus[strategy.strtype];
            return true;
        }
        public bool attachOutputBus(Bus b)
        {
            this.output = b;
            return true;
        }

        public bool startTracking()
        {
            
            if (input != null)
            {
                string uri = input.uri;
                if (uri.StartsWith("ws://"))
                {
                    
                    connection = new WebSocketSharp.WebSocket(uri);
                    {
                        connection.OnMessage += (sender, e) => processFrame(e.Data);
                        connection.OnClose += (sender, e) => stopTracking();
                        connection.Connect();
                        clientsverifier = new Thread(stopIfNoClients);
                        clientsverifier.Start();
                    }

                }
                return true;
            }
            else {
                Logging.Instance.Error(this.GetType(), "Input bus not attached");
                return false;
            }
        }

        public bool stopTracking()
        {
            if(connection!=null)
            connection.Close();
            connection = null;
            if(output!=null)
            output.Dispose();
            clientsverifier.Abort();
            return true;
        }

        
        public void processFrame(string data) {


            
            //riconverto i dati in giunti
            JointsFrameElement elem = JsonConvert.DeserializeObject<JointsFrameElement>(data);
            long intime = elem.timestamp;
            //estraggo l'array dei giunti
            JointElement[] joint = elem.joints;
            
            //array delle posizioni dei giunti appena arrivate
            double[] newframe = new double[3 * joint.Length];

            //velocità cinetica, i.e. distanza tra i due frame
            double movement;

            //matrice dei frame da riconoscere
            double[][] toclassifydata;

            //la label di output
            string output;


            //soglie di energia cinetica 
            double kinetic = 0.01;
            int stopthresh = 5;


            //creo il nuovo frame dei giunti
            for (int i = 0; i < joint.Length; i++)
            {
                newframe[i * 3] =     joint[i].Pos3d.x;
                newframe[i * 3 + 1] = joint[i].Pos3d.y;
                newframe[i * 3 + 2] = joint[i].Pos3d.z;
            }

            if (currentframe == null)
                currentframe = new double[3 * joint.Length];
            movement = computeDistance(currentframe, newframe);
            
            

            //sblocco l'acquisizione sempre, occhio alle threshold
            if (state == false && movement > kinetic)
            {
                state = true;
                currentframe = newframe;
            }


            //frame praticamente identico al precedente, non so se aggiornare comunque
            if (state == true && movement < kinetic) {
                stopcounter += 1;
                currentframe = newframe;
            }

            //sono in fase di acquisizione e sto riempiendo il buffer
            if (state == true && movement > kinetic && windoweddata.Count < Settings.Default.RECO_WINDOW_SIZE)
            {
                windoweddata.Add(newframe);
                stopcounter = 0;
                currentframe = newframe;
            }

            //eseguo il riconoscimento forzato perchè ho saturato il buffer oppure perchè sono fermo e ho un numero minimo di frames da classificare
            if (state== true && windoweddata.Count >Settings.Default.RECO_MINWINDOW_SIZE && (windoweddata.Count == Settings.Default.RECO_WINDOW_SIZE || stopcounter==stopthresh))
            {
                toclassifydata= windoweddata.ToArray();
                output = recognizeSample(windoweddata.ToArray());
                Console.WriteLine("Recognized gesture:" + output + "number of frames:" + windoweddata.Count);
                //svuoto il buffer
                currentframe = null;
                windoweddata.Clear();
                state = false;
                stopcounter = 0;
            }
            else if(state==true && stopcounter == stopthresh && windoweddata.Count < Settings.Default.RECO_MINWINDOW_SIZE)
            {
                //svuoto il buffer e lascio l'ultimo frame come riferimento
                currentframe = null;
                windoweddata.Clear();
                state = false;
                stopcounter = 0;
                //Console.WriteLine("Filtering noise");
            }



           


            long dff = DateTime.UtcNow.Ticks - intime;
            //Logging.Instance.Information(null, dff.ToString());
          
            
        }
        

        //campione in input, ritorna una stringa solo se il valore della prob è maggiore della soglia
        //base per lo streaming
        public string recognizeSample(double[][] input)
        {
            Dictionary<string,double> scores = strategy.classifyProbabilities(input);
            
            string x = scores.Where(e => e.Value == scores.Max(e2 => e2.Value)).First().Key;
            if (scores[x] > probthreshold)
                return x;
            else
                return "";
        }

        public double trainStrategy(double[][][] inputs, string[] outputs)
        {
            return strategy.train(inputs, outputs);
        }

        public double testStrategy(double[][][] inputs, string[] outputs) {
            return strategy.test(inputs,outputs);
        }


        private double computeDistance(double[] oldframe, double[] newframe) {

            double dist = 0.0;
            for(int i = 0; i < oldframe.Length; i++)
            {
                dist += Math.Pow(oldframe[i] - newframe[i],2);
            }
            return Math.Sqrt(dist);

        }
        //daemon which checks the presence of attached clients
        public void stopIfNoClients()
        {
            //20 seconds windows
            Thread.Sleep(20000);
            int totalclients = output.CountClients();
            Console.WriteLine("Clients:" + totalclients);
            if (totalclients == 0)
            {
                connection.Close();
                clientsverifier.Abort();
            }
            
        }

    }
}
