extern alias Kinect2;
using System;
using System.Collections.Generic;
using Kinect2.Microsoft.Kinect;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Windows;
using Managers.Busses;
using Utilities.Logger;
using System.Reflection;
using GestureInteractionServer.Properties;
using Models;
using Utilities;


namespace Managers.DevicesDriver
{
    public class Kinect2Driver : DeviceDriver
    {

        // Constant for clamping Z values of camera space points from being negative
        private const float InferredZPositionClamp = 0.1f;
        // Active Kinect sensor
        private KinectSensor kinectSensor = null;
        // Coordinate mapper to map one type of point to another
        private CoordinateMapper coordinateMapper = null;
        //The current latest frame
        private BodyFrame bodyFrame;
        private ColorFrame colorFrame;
        private DepthFrame depthFrame;
        // Reader for body frames
        private BodyFrameReader bodyFrameReader = null;
        //Reader for RGB and DEPTH
        private ColorFrameReader rgbreader =null;
        private DepthFrameReader depthreader = null;
        // Array for the bodies
        private Body[] bodies = null;
        /// Width of display (depth space)
        public int DisplayWidth { get; set; }
        /// Height of display (depth space)
        public int DisplayHeight { get; set; }

        private KinectJointFilter KinectJointFilter;


        public Kinect2Driver(String id, String name, String model, String producer,Dictionary<string,string[]> pars) : base(id, name, model, producer,pars)
        {
            databusparams[Settings.Default.DEV_OUTTYPE_SKELETON] = null;
          
            //riempio i parametri dei bus
            fillDataBusParamsKeys(pars);
        }


        public override bool start(String starttype)
        {
            
            bool started=base.start(starttype);

            if (started)
            {
                // one sensor is currently supported

                this.kinectSensor = KinectSensor.GetDefault();

                // get the coordinate mapper
                this.coordinateMapper = this.kinectSensor.CoordinateMapper;

                // get the depth (display) extents
                FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;
                this.depthreader = this.kinectSensor.DepthFrameSource.OpenReader();
                this.rgbreader = this.kinectSensor.ColorFrameSource.OpenReader();

                // get size of joint space
                this.DisplayWidth = frameDescription.Width;
                this.DisplayHeight = frameDescription.Height;

                //Default values:
                //float fSmoothing = 0.25f;
                //float fCorrection = 0.25f;
                //float fPrediction = 0.25f;
                //float fJitterRadius = 0.03f;
                //float fMaxDeviationRadius = 0.05f;
                float fSmoothing = 0.7f;
                float fCorrection = 0.4f;
                float fPrediction = 0.7f;
                float fJitterRadius = 0.1f;
                float fMaxDeviationRadius = 0.1f;
                this.KinectJointFilter = new KinectJointFilter(fSmoothing, fCorrection, fPrediction, fJitterRadius, fMaxDeviationRadius);

                // open the reader for the body frames
                this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

                // set IsAvailableChanged event notifier
                this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

                // open the sensor
                this.kinectSensor.Open();

                //wait some time to wake-up the kinect
                //CONTROLLARE SE VANNO BENE 2 SECONDI
                Thread.Sleep(2000);

                if (!this.kinectSensor.IsAvailable)
                {
                    dev_state = Settings.Default.DEVICE_STATE_NOT_CONNECTED;
                    _logger.Error(this.GetType(), "Failed initializing Kinect2Driver");
                    return false;
                }

                return true;
            }
            else
                return false;
        }


       
        private void pushJointData()
        {

            while (true)
            {
                using (BodyFrame tmp = bodyFrameReader.AcquireLatestFrame())
                {
                    
                    Body body;
                    if (tmp != bodyFrame && tmp!=null)
                    {
                        bodyFrame = tmp;
                    }
                    //sono ancora al frame precedente
                    else continue;
                    
                    this.bodies = new Body[bodyFrame.BodyCount];
                    bodyFrame.GetAndRefreshBodyData(this.bodies);


                    //selects the first body found
                    int trackedbody = -1;
                    for (int i = 0; i < this.bodies.Length; i++) {
                        if (this.bodies[i].IsTracked)
                        {
                            trackedbody = i;
                            break;
                        }
                    }
                    
                    if (trackedbody>=0)
                        body = this.bodies[trackedbody];
                    else continue;
                    


                    this.KinectJointFilter.UpdateFilter(body);
                    CameraSpacePoint[] joints = this.KinectJointFilter.GetFilteredJoints();
                    // convert the joint points to depth (display) space
                    Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();
                    foreach (JointType jointType in body.Joints.Keys)
                    {
                        // sometimes the depth(Z) of an inferred joint may show as negative
                        // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                        CameraSpacePoint position = joints[(int)jointType];
                        if (position.Z < 0)
                        {
                            position.Z = InferredZPositionClamp;
                        }

                        //From: https://msdn.microsoft.com/en-us/library/windowspreview.kinect.depthspacepoint.aspx
                        //"Depth space is the term used to describe a 2D location on the depth image"

                        DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                        jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                    }

                    //Console.WriteLine("sending skeleton data");
                    SkeletonInfo sk = PrepareSkeletonInfo(joints, jointPoints, body.HandLeftState, body.HandRightState, body.HandLeftConfidence, body.HandRightConfidence);
                    //standardizzo l'output 
                    JointsFrameElement jf = sk.ToJointsFrameElement();
                    bus[Settings.Default.DEV_OUTTYPE_SKELETON].receive(this, Settings.Default.DEV_OUTTYPE_SKELETON, JsonConvert.SerializeObject(jf));
                }
            }
        }

        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
           Logging.Instance.Information(this.GetType(),"[Kinect Status]: " + (this.kinectSensor.IsAvailable ? "Running" : "Kinect not available!"));
        }




        public override String enableWriteOnBus(string key)
        {
            //se il bus è gia attivo oppure non è gestibile per qualche motivo
            String prova = base.enableWriteOnBus(key);
            if (prova == Settings.Default.BUS_ALREADY_ACTIVATED || prova == Settings.Default.BUS_NOT_MANAGED)
                return prova;


            //se il bus invece è attivabile
            if (key == Settings.Default.DEV_OUTTYPE_SKELETON)
            {
                outthread[key] = new Thread(pushJointData);

                //istanziare con reflection
                bus[key] = bus[key] = (Bus)Activator.CreateInstance(TypeSolver.getTypeByName(databusparams[key][0])[0], databusparams[key][1], databusparams[key][2]);
                outthread[key].Start();
                return Settings.Default.BUS_ACTIVATED;
            }
            else return Settings.Default.BUS_NOT_MANAGED;

        }



      



        private SkeletonInfo PrepareSkeletonInfo(CameraSpacePoint[] joints, Dictionary<JointType, Point> jointPoints, HandState leftHandState, HandState rightHandState, TrackingConfidence leftHandConfidence, TrackingConfidence rightHandConfidence)
        {
            SkeletonInfo si = new SkeletonInfo();
            si.Joints = new SkeletonInfo.Skeleton();

            SkeletonInfo.Joint j;
            foreach (JointType jointType in jointPoints.Keys)
            {
                j = new SkeletonInfo.Joint();
                j.Name = Enum.GetName(typeof(JointType), jointType);
                j.X = joints[(int)jointType].X;
                j.Y = joints[(int)jointType].Y;
                j.Z = joints[(int)jointType].Z;
                j.DepthSpace = new SkeletonInfo.DepthSpaceJoint();
                j.DepthSpace.X = Math.Round(jointPoints[jointType].X);
                j.DepthSpace.Y = Math.Round(jointPoints[jointType].Y);

                if (jointType == JointType.HandLeft || jointType == JointType.HandRight)
                {
                    SkeletonInfo.HandJoint hj;
                    if (jointType == JointType.HandLeft)
                    {
                        hj = new SkeletonInfo.HandJoint(j, leftHandState, leftHandConfidence);
                    }
                    else
                    {
                        hj = new SkeletonInfo.HandJoint(j, rightHandState, rightHandConfidence);
                    }

                    si.SetJointValue(hj.Name, hj);
                }
                else
                {
                    si.SetJointValue(j.Name, j);
                }
            }

            return si;
        }


        public abstract class AbstractInfo
        {
            public enum DataType
            {
                InfoData,
                SkeletonData
            }

            [JsonProperty(PropertyName = "DataType")]
            [JsonConverter(typeof(StringEnumConverter))]
            public DataType DataType_ { get; set; }
        }

        public class SkeletonInfo : AbstractInfo
        {
            public class DepthSpaceJoint
            {
                public double X { get; set; }
                public double Y { get; set; }
            }

            public class Joint
            {
                public string Name { get; set; }
                public double X { get; set; }
                public double Y { get; set; }
                public double Z { get; set; }

                public DepthSpaceJoint DepthSpace { get; set; }
            }

            public class HandJoint : Joint
            {
                [JsonConverter(typeof(StringEnumConverter))]
                public HandState HandState { get; set; }
                [JsonConverter(typeof(StringEnumConverter))]
                public TrackingConfidence HandConfidence { get; set; }

                public HandJoint(Joint j, HandState hs, TrackingConfidence tc)
                {
                    this.Name = j.Name;
                    this.X = j.X;
                    this.Y = j.Y;
                    this.Z = j.Z;
                    this.DepthSpace = j.DepthSpace;

                    this.HandState = hs;
                    this.HandConfidence = tc;
                }
            }

            public class Skeleton
            {
                public Joint AnkleLeft { get; set; }
                public Joint AnkleRight { get; set; }
                public Joint ElbowLeft { get; set; }
                public Joint ElbowRight { get; set; }
                public Joint FootLeft { get; set; }
                public Joint FootRight { get; set; }
                public HandJoint HandLeft { get; set; }
                public HandJoint HandRight { get; set; }
                public Joint HandTipLeft { get; set; }
                public Joint HandTipRight { get; set; }
                public Joint Head { get; set; }
                public Joint HipLeft { get; set; }
                public Joint HipRight { get; set; }
                public Joint KneeLeft { get; set; }
                public Joint KneeRight { get; set; }
                public Joint Neck { get; set; }
                public Joint ShoulderLeft { get; set; }
                public Joint ShoulderRight { get; set; }
                public Joint SpineBase { get; set; }
                public Joint SpineMid { get; set; }
                public Joint SpineShoulder { get; set; }
                public Joint ThumbLeft { get; set; }
                public Joint ThumbRight { get; set; }
                public Joint WristLeft { get; set; }
                public Joint WristRight { get; set; }
            }

            public Skeleton Joints;

            public SkeletonInfo()
            {
                this.Joints = new Skeleton();

                this.DataType_ = DataType.SkeletonData;
            }

            public void SetJointValue(string jointName, Joint value)
            {
                this.Joints.GetType().GetProperty(jointName).SetValue(this.Joints, value);
            }

           


            public JointsFrameElement ToJointsFrameElement()
            {
                JointsFrameElement frame = new JointsFrameElement();
                frame.timestamp = DateTime.UtcNow.Ticks;
                PropertyInfo[] props = this.Joints.GetType().GetProperties();
                frame.joints = new JointElement[props.Length];

                //set data and push
                for (int i = 0; i < props.Length; i++)
                {
             
                    frame.joints[i] = new JointElement();
                   
                    if (props[i].PropertyType.ToString().Contains("HandJoint"))
                    {
                        HandJoint tmp = (HandJoint)props[i].GetValue(this.Joints);
                        frame.joints[i].Name = props[i].Name;
                        frame.joints[i].Pos2d = new Position2D(tmp.DepthSpace.X, tmp.DepthSpace.Y);
                        frame.joints[i].Pos3d = new Position3D(tmp.X, tmp.Y, tmp.Z);
                        frame.joints[i].Velox = new Velocity(0, 0, 0);
                        frame.joints[i].Orient = new Orientation(0, 0, 0, 1);
                        frame.joints[i].State = tmp.HandState.ToString();
                        if (tmp.HandConfidence.ToString() == "Low")
                        {
                            frame.joints[i].Confidence = 0;
                        }
                        else
                            frame.joints[i].Confidence = 100;
                    }
                    else {
                        Joint tmp = (Joint)props[i].GetValue(this.Joints);
                        frame.joints[i].Name = props[i].Name;
                        frame.joints[i].Pos2d = new Position2D(tmp.DepthSpace.X, tmp.DepthSpace.Y);
                        frame.joints[i].Pos3d = new Position3D(tmp.X, tmp.Y, tmp.Z);
                        frame.joints[i].Velox = new Velocity(0, 0, 0);
                        frame.joints[i].Orient = new Orientation(0, 0, 0, 1);
                        frame.joints[i].State = "undefined";
                        frame.joints[i].Confidence = -1;
                    }

                   
                }
                return frame;
            }
        }

    }
}