using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Models
{
    public class DeviceCollectionElement
    {
        public string _id;
        public string name;
        public string model;
        public string producer;
    }



    public class Pos2DFrameElement
    {
        public double x { get; set; }
        public double y { get; set; }
        public long timestamp;


        public Pos2DFrameElement(double x, double y, long timestamp)
        {
            this.x = x;
            this.y = y;
            this.timestamp = timestamp;
        }


    }
    public class Pos2DCollectionElement
    {
        public String tag;
        public String device_id;
        public String stream;
        public Pos2DFrameElement[] frames;

    }



    public class RGBFrameElement
    {
        public long timestamp;
        //TODO
        public string content;
    }

    public class DepthFrameElement
    {
        public long timestamp;
        //TODO
        public string content;
    }

    public class StreamsCollectionElement
    {
        public string _id;
        public string type;
    }

    public class JointsFrameElement
    {

        public JointElement[] joints;
        public long timestamp;

        
    }
    public class JointGestureCollectionElement
    {
        
        public ObjectId _id;
        public String tag;
        public String device_id;
        public String stream;
        public JointsFrameElement[] frames;

    }

    public class JointElement
    {
        public string Name { set; get; }
        public Position3D Pos3d { set; get; }
        public Position2D Pos2d { set; get; }
        public int Confidence { set; get; }
        public string State { set; get; }
        public Velocity Velox { set; get; }
        public Orientation Orient { set; get; }
    }

    public class Position3D
    {
        public Position3D(double x, double y, double z)
        {
            this.x = x;
            this.z = z;
            this.y = y;
        }
        public double x;
        public double y;
        public double z;
    }

    public class Velocity
    {
        public Velocity(double vx, double vy, double vz)
        {
            this.vx = vx;
            this.vz = vz;
            this.vy = vy;
        }
        public double vx;
        public double vy;
        public double vz;
    }
    public class Position2D
    {
        public Position2D(double x, double y)
        {
            this.x = x;
            this.y = y;
        }
        public double x;
        public double y;
    }
    public class Orientation
    {

        public Orientation(double x, double y, double z, double w)
        {
            this.x = x;
            this.z = z;
            this.y = y;
            this.w = w;
        }

        public double x;
        public double y;
        public double z;
        public double w;
    }
}

