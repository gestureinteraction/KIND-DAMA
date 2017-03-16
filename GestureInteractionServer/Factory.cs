using System;
using System.Collections.Generic;
using Managers.DevicesDriver;
using Managers;
using System.Diagnostics;
using Utilities.Logger;
using MongoDB.Bson;
using System.Web.Http.SelfHost;
using System.Web.Http;
using System.Net.Http.Formatting;
using Utilities;
using Newtonsoft.Json.Linq;
using System.IO;
using Recognition;
using Managers.Busses;

public class Factory
{


    private Process mongodproc;
    private dynamic json;


    private static void Main(string[] args)
    {
        Factory f = new Factory();
        //leggo il file xml di configurazione
        f.readConfigJson(args[0]);
        //carico il logger
        f.startLogger();
        //avvio mongod
        f.startmongodb();
        //avvio dbmanager
        f.startDBManager();
        //carico i devices
        f.loadDevices();
        //carico la recognizerfacade
        f.loadRecognizerFacade();
        //avvio il server selfhost
        f.startServer();

        Console.ReadKey();
    }


    public void loadRecognizerFacade()
    {
        //reads properties from json file
        //for each device reads training data

        //builds a pool of output busses
        List < Tuple<string,string,Bus>> busses=new List<Tuple<string, string, Bus>>();
        double probthresh;
        JArray jbusses;
        try
        {  jbusses= json.configuration.recognitionmanager.busses; }
        catch
        { jbusses = new JArray() { json.configuration.recognitionmanager.busses }; }

        //creo i bus di output per ciascuno stream dei device
        for(int i = 0; i < jbusses.Count; i++)
        {
            dynamic element = jbusses[i];
            string id = element.id.Value;
            string stream = element.stream.Value;
            string bustype = element.busdriverclass.Value;
            string ip = element.ip.Value;
            string port = element.port.Value;
            Bus b=(Bus)Activator.CreateInstance(TypeSolver.getTypeByName(bustype)[0], new object[] {ip,port });

            busses.Add(new Tuple<string, string, Bus>(id,stream,b));
        }
        probthresh = json.configuration.recognitionmanager.probthresh;

        RecognitionManager.Instance.init(busses,probthresh);
        
    }

    public void readConfigJson(string configfile)
    {
        json = JObject.Parse(File.ReadAllText(configfile));
    }
    public void startLogger()
    {
        Trace.Listeners.Add(new CustomConsoleTraceListener());
    }
    public void startmongodb()
    {
        string procpath = json.configuration.mongo.processpath.Value;
        string procpars = json.configuration.mongo.parameters.Value;
        mongodproc = Process.Start(new ProcessStartInfo(procpath, procpars));
    }
    public void startDBManager()
    {
        string dbname = json.configuration.dbmanager.dbname.Value;
        string dbip = json.configuration.dbmanager.ip.Value;
        string dbport = json.configuration.dbmanager.port.Value;
        DBManager.Instance.Init(dbname, dbip, Int32.Parse(dbport));
    }

    public void loadDevices()
    {

        JArray devs;

        try
        { devs = json.configuration.devices.device; }
        catch
        { devs = new JArray() { json.configuration.devices.device }; }
        //creo i devices
        
       
        for (int i = 0; i < devs.Count; i++)
        {

            dynamic element = devs[i];
            string id = element.id.Value;
            string driv = element.driverclass.Value;

            Dictionary<string, string[]> startParams = new Dictionary<string, string[]>();
           
            JArray streams;
            try
            {
                streams = element.streams.stream;
            }
            catch
            {
               streams = new JArray() { element.streams.stream };
            }
            
            int streamcount = streams.Count;
            for (int j = 0; j < streamcount; j++)
            {
                dynamic stelem = streams[j];
                string chiave = stelem.name.Value;
                string streamdriver = stelem.busdriverclass.Value;
                string ip = stelem.ip.Value;
                string port = stelem.port.Value;
                string[] pars = new string[] { streamdriver, ip, port };
                startParams[chiave] = pars;
            }
            //controllo se il dispositivo è in db

            BsonValue[] data = DBManager.Instance.queryDeviceId(id);

            if (data.Length > 0)
            {
                //data[0]=name, data[1]=model, data[2]=producer
                try
                {
                    DevicesManager.Instance.addDevice((DeviceDriver)Activator.CreateInstance(TypeSolver.getTypeByName(driv)[0], new object[] { id, (string)data[0], (string)data[1], (string)data[2], startParams }));
                }
                catch { Logging.Instance.Error(GetType(), "Errore istanziazione driver {0}",(string)data[0]); }

            }
        }
    }
    public void startServer()
    {
        string ip = json.configuration.controller.ip.Value; 
        string port = json.configuration.controller.port.Value; ;
        // Servizi e configurazione dell'API Web
        var config = new HttpSelfHostConfiguration(new Uri("http://"+ip+":"+port));

        // Route dell'API Web
        config.MapHttpAttributeRoutes();
        config.Routes.MapHttpRoute(name: "DefaultApi",routeTemplate: "{controller}/{action}",defaults: new { action = RouteParameter.Optional });
        //JSON output
        config.Formatters.Clear();
        config.Formatters.Add(new JsonMediaTypeFormatter());
        //Enables readings from outside this context
        config.EnableCors();
        //Enlarge maximum request size
        config.MaxReceivedMessageSize = 2147483647;


        using (var server = new HttpSelfHostServer(config))
        {
            server.OpenAsync().Wait();
            Logging.Instance.Information(this.GetType(), "Server is open at: {0}", config.BaseAddress);
            Console.ReadKey();
        }
    }
}
