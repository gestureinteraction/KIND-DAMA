using System;
using MongoDB.Driver;
using MongoDB.Bson;
using Utilities.Logger;
using GestureInteractionServer.Properties;
using Models;
using System.Collections.Generic;
using Recognition;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Recognition.ClStrategies;
using System.Linq;

namespace Managers
{
    public class DBManager
    {
        private static DBManager instance;
        private IMongoClient _client;
        private IMongoDatabase _db;

        private DBManager() {
            _client = null;
            _db = null;
        }

        public static DBManager Instance
        {
            
            get
            {
               
                if (instance == null)
                {
                    instance = new DBManager();
                }
                return instance;
            }
        }


        public void Init(String dbname, String ip, int port)
        {
           
            //accetta in ingresso l'ip e la porta del mongoserver
            string connectionString = "mongodb://"+ip+":"+port;
            
            _client = new MongoClient(connectionString);
            
            _db = _client.GetDatabase(dbname);
            
            try {
                _db.ListCollections().MoveNext();
            }
            catch(Exception e)
            {
                Logging.Instance.Information(this.GetType(), e.ToString(), null);
                throw e;
//                return;
            }
        }


        public String CollectionsToString()
        {
            String output="";
            var coll1 = getCollection(Settings.Default.DB_DEVICES_COLLECTION);
            var cursor = coll1.Find(new BsonDocument()).ToList();
            
            foreach(var x in cursor)
                output += x+"\n";


            coll1 = getCollection(Settings.Default.DB_STREAMS_COLLECTION);
            cursor = coll1.Find(new BsonDocument()).ToList();

            foreach (var x in cursor)
                output += x + "\n";

            coll1 = getCollection(Settings.Default.DB_GESTURES_COLLECTION);
            cursor = coll1.Find(new BsonDocument()).ToList();

            foreach (var x in cursor)
                output += x + "\n";
            return output;

        }

        public IMongoCollection<BsonDocument> getCollection(String collect)
        {
            
            IMongoCollection<BsonDocument> coll;
            coll = _db.GetCollection<BsonDocument>(collect);
            return coll;
        }

        public List<ClassifierStrategy> retrieveStrategies()
        {
            List<ClassifierStrategy> clss = new List<ClassifierStrategy>();
            IMongoCollection<BsonDocument> docs = getCollection(Settings.Default.DB_STRATEGIES_COLLECTION);
            var result = docs.Find(new BsonDocument()).ToList();
            for (int i = 0; i < result.Count; i++)
            {
                BsonValue b;
                result[i].TryGetValue("_t", out b);
                if (b.ToString() == "HMMStrategy")
                {
                    result[i].TryGetValue("dev_id", out b);
                    string did = b.ToString();
                    result[i].TryGetValue("strtype", out b);
                    string strtype = b.ToString();
                    //genera errore di lettura, ma materialmente funziona in quanto i parametri di inizializzazione
                    //per l'hmm non sono più necessari
                    Logging.Instance.Information(this.GetType(), "Warning:I'm loading strategy without training parameters.This is not an error.");
                    HMMStrategy m = new HMMStrategy(did, strtype, new object[0]);
                    result[i].TryGetValue("binhmm", out b);
                    m.binhmm = (byte[]) b.RawValue;
                    m.debinarizemodel();
                    result[i].TryGetValue("index2label", out b);
                    BsonArray tmp = b.AsBsonArray;

                    m.index2label = new string[b.AsBsonArray.Count];
                    for (int j = 0; i < b.AsBsonArray.Count; i++)
                        m.index2label[j] = b.AsBsonArray[j].ToString();
                    clss.Add(m);
                }

            }
            
            return clss;
        }

        private List<BsonDocument> getGestureSet(string id_dev, string strtype, string gestureTag)
        {
            //recupera i gesti 
            IMongoCollection<BsonDocument> gestures = (IMongoCollection<BsonDocument>)DBManager.Instance.getCollection(Settings.Default.DB_GESTURES_COLLECTION);
            //li formatta opportunamente
            List<FilterDefinition<BsonDocument>> l = new List<FilterDefinition<BsonDocument>>();
            l.Add(Builders<BsonDocument>.Filter.Eq("device_id", id_dev));
            l.Add(Builders<BsonDocument>.Filter.Eq("stream", strtype));
            l.Add(Builders<BsonDocument>.Filter.Eq("tag", gestureTag));

            //seleziona solo i dati provenienti da un dispositivo e da uno stream specifico
            return (gestures.Find(Builders<BsonDocument>.Filter.And(l)).ToList());

        }

        public Tuple<double[][][],string[]> retrieve3DGestureSet(string id_dev, string strtype)
        {
            //recupera i gesti 
            IMongoCollection<BsonDocument> gestures = (IMongoCollection<BsonDocument>)DBManager.Instance.getCollection(Settings.Default.DB_GESTURES_COLLECTION);
            //li formatta opportunamente
            List<FilterDefinition<BsonDocument>> l = new List<FilterDefinition<BsonDocument>>();
            l.Add(Builders<BsonDocument>.Filter.Eq("device_id", id_dev));
            l.Add(Builders<BsonDocument>.Filter.Eq("stream", strtype));
           
            //seleziona solo i dati provenienti da un dispositivo e da uno stream specifico
            var result = gestures.Find(Builders<BsonDocument>.Filter.And(l)).ToList();
            

            
            double[][][] inputs = new double[result.Count][][];
            string[] tags=new string[result.Count];
            for (int i=0;i< result.Count; i++)
            {

                BsonValue tag;
                BsonValue frames;
                result[i].TryGetValue("tag", out tag);
                tags[i] = tag.ToString();
                result[i].TryGetValue("frames", out frames);
                BsonArray frs = frames.AsBsonArray;

                inputs[i] = new double[frs.Count][];
                
               
                for (int j = 0; j < frs.Count; j++)
                {
                   BsonValue joints;
                   frs[j].ToBsonDocument().TryGetValue("joints", out joints);
                   BsonArray jts = joints.AsBsonArray;
                   inputs[i][j] = new double[3*jts.Count];

                   for(int k=0;k< jts.Count-2; k++)
                   {
                        BsonValue pos;
                        jts[k].ToBsonDocument().TryGetValue("Pos3d", out pos);
                        BsonValue x;
                        BsonValue y;
                        BsonValue z;
                        pos.ToBsonDocument().TryGetValue("x", out x);
                        pos.ToBsonDocument().TryGetValue("y", out y);
                        pos.ToBsonDocument().TryGetValue("z", out z);
                        inputs[i][j][k*3] = (double) x;
                        inputs[i][j][k*3+1] = (double) y;
                        inputs[i][j][k*3+2] = (double) z;
                   } 
                }
            }

            return new Tuple<double[][][], string[]>(inputs, tags);
        }

        public JointGestureCollectionElement retrieve3DGesture(string id_dev, string strtype, string gestureTag, int dbPosition)
        {
            try
            {
                var result = getGestureSet(id_dev, strtype, gestureTag);
                JointGestureCollectionElement gesture = new JointGestureCollectionElement();
                gesture.device_id = id_dev;
                gesture.stream = strtype;


                //in futuro utilizzare il campo ObjectID della joint gesture collection element
                //BsonValue _id;
                //result[dbPosition].TryGetValue("_id", out _id);
                //gesture.id = _id.ToString();

                BsonValue tag;
                result[dbPosition].TryGetValue("tag", out tag);
                gesture.tag = tag.ToString();

                BsonValue frames;
                result[dbPosition].TryGetValue("frames", out frames);
                BsonArray frs = frames.AsBsonArray;
                gesture.frames = new JointsFrameElement[frs.Count];

                for (int j = 0; j < frs.Count; j++)
                {
                    gesture.frames[j] = new JointsFrameElement();
                    BsonValue timestamp;
                    frs[j].ToBsonDocument().TryGetValue("timestamp", out timestamp);
                    gesture.frames[j].timestamp = (long)timestamp.ToDouble();

                    BsonValue joints;
                    frs[j].ToBsonDocument().TryGetValue("joints", out joints);
                    BsonArray jts = joints.AsBsonArray;
                    gesture.frames[j].joints = new JointElement[jts.Count];

                    for (int k = 0; k < jts.Count; k++)
                    {
                        gesture.frames[j].joints[k] = new JointElement();
                        BsonValue Name;
                        jts[k].ToBsonDocument().TryGetValue("Name", out Name);
                        gesture.frames[j].joints[k].Name = Name.ToString();

                        BsonValue Pos3d;
                        jts[k].ToBsonDocument().TryGetValue("Pos3d", out Pos3d);
                        BsonValue x;
                        BsonValue y;
                        BsonValue z;
                        Pos3d.ToBsonDocument().TryGetValue("x", out x);
                        Pos3d.ToBsonDocument().TryGetValue("y", out y);
                        Pos3d.ToBsonDocument().TryGetValue("z", out z);
                        gesture.frames[j].joints[k].Pos3d = new Position3D(x.ToDouble(), y.ToDouble(), z.ToDouble());

                        BsonValue Pos2d;
                        jts[k].ToBsonDocument().TryGetValue("Pos2d", out Pos2d);
                        Pos2d.ToBsonDocument().TryGetValue("x", out x);
                        Pos2d.ToBsonDocument().TryGetValue("y", out y);
                        gesture.frames[j].joints[k].Pos2d = new Position2D(x.ToDouble(), y.ToDouble());

                        BsonValue Confidence;
                        jts[k].ToBsonDocument().TryGetValue("Confidence", out Confidence);
                        gesture.frames[j].joints[k].Confidence = Confidence.ToInt32();

                        BsonValue State;
                        jts[k].ToBsonDocument().TryGetValue("State", out State);
                        gesture.frames[j].joints[k].State = State.ToString();

                        BsonValue Velox;
                        jts[k].ToBsonDocument().TryGetValue("Velox", out Velox);
                        BsonValue vx;
                        BsonValue vy;
                        BsonValue vz;
                        Velox.ToBsonDocument().TryGetValue("vx", out vx);
                        Velox.ToBsonDocument().TryGetValue("vy", out vy);
                        Velox.ToBsonDocument().TryGetValue("vz", out vz);
                        gesture.frames[j].joints[k].Velox = new Velocity(vx.ToDouble(), vy.ToDouble(), vz.ToDouble());

                        BsonValue Orient;
                        jts[k].ToBsonDocument().TryGetValue("Orient", out Orient);
                        BsonValue w;
                        Orient.ToBsonDocument().TryGetValue("x", out x);
                        Orient.ToBsonDocument().TryGetValue("y", out y);
                        Orient.ToBsonDocument().TryGetValue("z", out z);
                        Orient.ToBsonDocument().TryGetValue("w", out w);
                        gesture.frames[j].joints[k].Orient = new Orientation(x.ToDouble(), y.ToDouble(), z.ToDouble(), w.ToDouble());
                    }
                }

                return gesture;
            }
            catch (Exception e)
            {
                Logging.Instance.Error(this.GetType(), e);
                return null;
            }
        }

        public int retrieve3DGesturesCounter(string id_dev, string strtype, string gestureTag)
        {
            var result = getGestureSet(id_dev, strtype, gestureTag);
            try
            {
                return result.Count;
            }
            catch (Exception e)
            {
                Logging.Instance.Error(this.GetType(), e);
                return 0;
            }
        }


        public BsonValue[] queryDeviceId(string id)
        {
            BsonValue[] output=new BsonValue[0];
            IMongoCollection<BsonDocument> devices = getCollection(Settings.Default.DB_DEVICES_COLLECTION);
            var filterdev = Builders<BsonDocument>.Filter.Eq("_id", id);
            var result = devices.Find(filterdev).ToList();
            if (result.Count ==1) {
                output = new BsonValue[3];
                result[0].TryGetValue("name", out output[0]);
                result[0].TryGetValue("model", out output[1]);
                result[0].TryGetValue("producer", out output[2]);
            }

            return output;
            
        }


        public Boolean insertStream(String _id, String type)
        {
            IMongoCollection<StreamsCollectionElement> coll;
            coll = _db.GetCollection<StreamsCollectionElement>(Settings.Default.DB_STREAMS_COLLECTION);
            StreamsCollectionElement elem = new StreamsCollectionElement();
            elem._id = _id;
            elem.type = type;
           
            try
            {
                coll.InsertOne(elem);
            }
            catch (Exception e)
            {
                Logging.Instance.Error(this.GetType(), e);
                return false;
            }

            return true;
        }

        public Boolean insertDevice(String _id, String name, String model, String producer)
        {
            IMongoCollection<DeviceCollectionElement> coll;
            coll = _db.GetCollection<DeviceCollectionElement>(Settings.Default.DB_DEVICES_COLLECTION);
            DeviceCollectionElement elem = new DeviceCollectionElement();
            elem._id = _id;
            elem.name = name;
            elem.model = model;
            elem.producer = producer;
            try
            {
                coll.InsertOne(elem);
            }
            catch (Exception e) {
                Logging.Instance.Error(this.GetType(),e);
                return false;
            }
                
            return true;
        }

        public Boolean insertStrategy(ClassifierStrategy s)
        {
            IMongoCollection<ClassifierStrategy> coll;
            coll = _db.GetCollection<ClassifierStrategy>(Settings.Default.DB_STRATEGIES_COLLECTION);
            try
            {
                
                coll.InsertOne(s);
                return true;
            }
            catch (Exception e)
            {
                Logging.Instance.Error(this.GetType(), e);
                return false;
            }
        }

        public Boolean insertGesture(String tag, String device_id, String streamtype, JointsFrameElement[] frames)
        {
            IMongoCollection<JointGestureCollectionElement> coll;
            coll = _db.GetCollection<JointGestureCollectionElement>(Settings.Default.DB_GESTURES_COLLECTION);
            JointGestureCollectionElement elem = new JointGestureCollectionElement();
            elem.tag = tag;
            elem.frames = new JointsFrameElement[frames.Length];
            
            for (int i=0;i<frames.Length;i++)
            {
                elem.frames[i] = new JointsFrameElement();
                elem.frames[i].timestamp = frames[i].timestamp;
                elem.frames[i].joints = frames[i].joints.OrderBy(c => c.Name).ToArray();
            }
           
            elem.device_id = device_id;
            elem.stream = streamtype;

            //controllo se il dev_id e streamtype sono già noti
            IMongoCollection<BsonDocument> dev_coll = getCollection(Settings.Default.DB_DEVICES_COLLECTION);
            IMongoCollection<BsonDocument> streams_coll = getCollection(Settings.Default.DB_STREAMS_COLLECTION);

            var filterdev = Builders<BsonDocument>.Filter.Eq("_id", device_id);
            var devices = dev_coll.Find(filterdev).ToList();
            var filterstr = Builders<BsonDocument>.Filter.Eq("type", streamtype);
            var streams = streams_coll.Find(filterstr).ToList();

            
            if (devices.Count>0 && streams.Count>0)
            {
                
                try
                {
                    coll.InsertOne(elem);
                }
                catch (Exception e)
                {
                    Logging.Instance.Error(this.GetType(), e);
                    return false;
                }

                return true;
            }
            else
            {
                
                return false;
            }
           

        }


    }





}
