extern alias Kinect1;
using Utilities;
using GestureInteractionServer.Properties;
using Utilities.Logger;
using Managers.Busses;
using Managers.DevicesDriver;
using Managers.DevicesDriver.Kinect1DriverSupport;
using Managers.DevicesDriver.Kinect1DriverSupport.HandStatusDetector;
using Kinect1.Microsoft.Kinect;
using Kinect1.Microsoft.Kinect.Toolkit.Interaction;
using Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Managers.DevicesDriver
{
    public class Kinect1Driver:DeviceDriver
    {
     
        private KinectSensor kinectSensor;

        public int DisplayWidth { get; private set; }
        public int DisplayHeight { get; private set;  }

        private HandStatus LeftHandStatus = HandStatus.Released;
        private HandStatus RightHandStatus = HandStatus.Released;

        private HandStatusDetector hsd;
        private Skeleton oldplayerskeleton;
        private Skeleton newplayerskeleton;

        public Kinect1Driver(String id, String name, String model, String producer, Dictionary<string, string[]> pars) : base(id, name, model, producer, pars)
        {
            KinectSensor.KinectSensors.StatusChanged += new EventHandler<StatusChangedEventArgs>(KinectSensors_StatusChanged);
           
            //properties driver dependent
            databusparams[Settings.Default.DEV_OUTTYPE_SKELETON] = null;
            DisplayWidth = 640;
            DisplayHeight = 480;
            //riempio i parametri dei bus
            fillDataBusParamsKeys(pars);
        }

       

        private void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {

            Logging.Instance.Information(this.GetType(),"Kinect status:" + e.ToString());
        }

        public override bool start(String starttype)
        {
            
            bool notConnected = base.start(starttype);
            
            if (notConnected)
            {
                
                if (KinectSensor.KinectSensors.Count > 0)
                {
                    
                    this.kinectSensor = KinectSensor.KinectSensors[0];
                    
                    // Skeleton Stream
                    this.kinectSensor.SkeletonStream.Enable(new TransformSmoothParameters()
                    {
                        Smoothing = 0.5f,
                        Correction = 0.1f,
                        Prediction = 0.5f,
                        JitterRadius = 0.1f,
                        MaxDeviationRadius = 0.1f
                    });
                    this.kinectSensor.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(kinectSensor_SkeletonFrameReady);
                   

                    // Initialize hand status detector before starting Kinect
                   
                    Logging.Instance.Information(null, "entro", null);
                    try
                    {
                        this.kinectSensor.Start();
                        return true;

                    }
                    catch
                    {
                        Logging.Instance.Error(this.GetType(), "Unable to start the Kinect Sensor v1");
                        return false;
                    }
                }
                else {
                    Logging.Instance.Error(this.GetType(), "Unable to start the Kinect Sensor v1");
                    return false;
                }
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



            if (key == Settings.Default.DEV_OUTTYPE_SKELETON)
            {
                outthread[key] = new Thread(pushJointData);
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

        public void pushJointData()
        {
            
            while (true)
            {
                if (oldplayerskeleton != newplayerskeleton && newplayerskeleton!=null)
                {
                    oldplayerskeleton = newplayerskeleton;
                    JointsFrameElement elem = ToJointsFrameElement(newplayerskeleton.Joints);
                    //ATTENZIONE COLLO DI BOTTIGLIA!!!!
                    bus[Settings.Default.DEV_OUTTYPE_SKELETON].receive(this, Settings.Default.DEV_OUTTYPE_SKELETON, JsonConvert.SerializeObject(elem));
                    
                }
             }
        }

        private void hsd_HandStatusDetected(object sender, HandStateInfo handStateInfo)
        {
            if (handStateInfo.handSide == HandSide.Left)
            {
                this.LeftHandStatus = handStateInfo.handStatus;
            } else if (handStateInfo.handSide == HandSide.Right)
            {
                this.RightHandStatus = handStateInfo.handStatus;
            }
        }

        private Skeleton TrackClosestSkeleton(Skeleton[] skeletonData)
        {
            float closestDistance = 10000f; // Start with a far enough distance
            Skeleton closestSkeleton = null;
            foreach (Skeleton skeleton in skeletonData.Where(s => s.TrackingState != SkeletonTrackingState.NotTracked))
            {
                if (skeleton.Position.Z < closestDistance)
                {
                    closestSkeleton = skeleton;
                    closestDistance = skeleton.Position.Z;
                }
            }

            return closestSkeleton;
        }

        private void kinectSensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    Skeleton[] skeletonData = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletonData);

                    //Skeleton playerSkeleton = (from s in skeletonData where s.TrackingState == SkeletonTrackingState.Tracked select s).FirstOrDefault();

                    newplayerskeleton = TrackClosestSkeleton(skeletonData);
                   
                }
            }
        }

        private JointsFrameElement ToJointsFrameElement(JointCollection joints)
        {
            SkeletonInfo si = new SkeletonInfo();
            si.Joints = new SkeletonInfo.Skeleton();

            

            JointsFrameElement frame=new JointsFrameElement();
            frame.timestamp = DateTime.UtcNow.Ticks;
            frame.joints = new JointElement[joints.Count];

            int i = 0;
            foreach (Joint joint in joints)
            {

                frame.joints[i] = new JointElement();
                frame.joints[i].Name= ConvertJointTypeToSDK2JointName(joint.JointType);
                frame.joints[i].Pos3d = new Position3D(joint.Position.X, joint.Position.Y, joint.Position.Z);
                
                DepthImagePoint dip = this.kinectSensor.CoordinateMapper.MapSkeletonPointToDepthPoint(joint.Position, DepthImageFormat.Resolution640x480Fps30);
                frame.joints[i].Pos2d = new Position2D(dip.X, dip.Y);

                frame.joints[i].Velox = new Velocity(0, 0, 0);
                frame.joints[i].Orient = new Orientation(0, 0, 0, 0);
                frame.joints[i].State = "undefined";
                frame.joints[i].Confidence = -1;

                // Set hand state
                /*if (joint.JointType == JointType.HandLeft || joint.JointType == JointType.HandRight)
                {

                    if (this.LeftHandStatus == HandStatus.Gripped)
                    {
                        frame.joints[i].State = "Closed";
                    }
                    else if (this.LeftHandStatus == HandStatus.Released)
                        frame.joints[i].State = "Open";
                }*/
                i++;
            }


           
            return frame;
        }

        private string ConvertJointTypeToSDK2JointName(JointType jt)
        {
            string res = null;
            switch (jt)
            {
                case JointType.HipCenter:
                    res = "SpineBase";
                    break;
                case JointType.Spine:
                    res = "SpineMid";
                    break;
                case JointType.ShoulderCenter:
                    res = "SpineShoulder";
                    break;
                default:
                    res = Enum.GetName(typeof(JointType), jt);
                    break;
            }

            return res;
        }
    }
}
