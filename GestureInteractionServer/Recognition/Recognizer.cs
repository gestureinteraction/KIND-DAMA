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


        public Recognizer(ClassifierStrategy strategy, double probthreshold)
        {
            this.strategy = strategy;
            this.probthreshold = probthreshold;
            input = null;
            output = null;
            
            windoweddata = new List<double[]>();
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

            for (int ndevs = 1; ndevs <= 3; ndevs++)
            {
                JointsFrameElement elem = JsonConvert.DeserializeObject<JointsFrameElement>(data);
                long intime = elem.timestamp;
                JointElement[] joint = elem.joints;
                double x, y, z;
                double[] fr = new double[3 * joint.Length];
                for (int i = 0; i < joint.Length; i++)
                {
                    x = joint[i].Pos3d.x;
                    y = joint[i].Pos3d.y;
                    z = joint[i].Pos3d.z;
                    fr[i * 3] = x;
                    fr[i * 3 + 1] = y;
                    fr[i * 3 + 2] = z;
                }
                if (windoweddata.Count < Settings.Default.RECO_WINDOW_SIZE)
                    windoweddata.Add(fr);
                else
                {
                    windoweddata.Insert(0, fr);
                    windoweddata.RemoveAt(windoweddata.Count - 1);
                }
                double[][] toclassifydata = windoweddata.ToArray();
                string output = recognizeSample(windoweddata.ToArray());
                long dff = DateTime.UtcNow.Ticks - intime;
                Logging.Instance.Information(null, dff.ToString());
            }

            
            //una riga equivale ad un frame

            //trasformare il dato in double[][] e passarlo a recognizeSample
            //se riconosce un gesto lo scrive in output sul bus
            //dicendo il tag e il timestamp del primo e ultimo frame del gesto
            //altrimenti continua  a riempire il buffer
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
