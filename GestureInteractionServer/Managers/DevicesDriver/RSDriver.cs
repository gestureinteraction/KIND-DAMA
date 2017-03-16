using System;
using Newtonsoft.Json;
using System.Threading;
using System.Collections.Generic;
using Managers.Busses;
using Utilities.Logger;
using System.Reflection;
using GestureInteractionServer.Properties;
using Models;
using Utilities;

namespace Managers.DevicesDriver
{
    public class RSDriver : DeviceDriver
    {
        private PXCMSession session { get; set; }
        private PXCMSenseManager sm { get; set; }
        private PXCMHandModule handModule { get; set; }
        private PXCMHandConfiguration handConfig { get; set; }
        private PXCMHandData handData { get; set; }

        private PXCMHandData.IHand ihand;


        public int DisplayWidth { get; set; }
        public int DisplayHeight { get; set; }

        public RSDriver(String id, String name, String model, String producer,Dictionary<string,string[]> pars) : base(id, name, model, producer,pars)
        {
            //properties driver dependent
            databusparams[Settings.Default.DEV_OUTTYPE_SKELETON] = null;
            DisplayWidth = 640;
            DisplayHeight = 480;
            //riempio i parametri dei bus
            fillDataBusParamsKeys(pars);

        }

        public override bool start(String starttype){
            bool notConnected = base.start(starttype);

            if (notConnected)
            {

                
                ////se non è attivo avanzo con la connessione al dispositivo
                session = PXCMSession.CreateInstance();
                

                sm = PXCMSenseManager.CreateInstance();

                // Enable hand tracking in the pipeline
                sm.EnableHand();

                // Get a hand instance here (or inside the AcquireFrame/ReleaseFrame loop) for querying capabilities
                handModule = sm.QueryHand();



                //********************GESTIONE CONFIGURAZIONE********************
                // Get an instance of PXCMHandConfiguration
                handConfig = handModule.CreateActiveConfiguration();
                int time = 0;
                // Make configuration changes and apply them
                handConfig.SetTrackingMode(PXCMHandData.TrackingModeType.TRACKING_MODE_FULL_HAND);
                handConfig.EnableTrackedJoints(true);
                handConfig.EnableJointSpeed(PXCMHandData.JointType.JOINT_CENTER, PXCMHandData.JointSpeedType.JOINT_SPEED_ABSOLUTE, time);
                handConfig.EnableJointSpeed(PXCMHandData.JointType.JOINT_INDEX_BASE, PXCMHandData.JointSpeedType.JOINT_SPEED_ABSOLUTE, time);
                handConfig.EnableJointSpeed(PXCMHandData.JointType.JOINT_INDEX_JT1, PXCMHandData.JointSpeedType.JOINT_SPEED_ABSOLUTE, time);
                handConfig.EnableJointSpeed(PXCMHandData.JointType.JOINT_INDEX_JT2, PXCMHandData.JointSpeedType.JOINT_SPEED_ABSOLUTE, time);
                handConfig.EnableJointSpeed(PXCMHandData.JointType.JOINT_INDEX_TIP, PXCMHandData.JointSpeedType.JOINT_SPEED_ABSOLUTE, time);
                handConfig.EnableJointSpeed(PXCMHandData.JointType.JOINT_MIDDLE_BASE, PXCMHandData.JointSpeedType.JOINT_SPEED_ABSOLUTE, time);
                handConfig.EnableJointSpeed(PXCMHandData.JointType.JOINT_MIDDLE_JT1, PXCMHandData.JointSpeedType.JOINT_SPEED_ABSOLUTE, time);
                handConfig.EnableJointSpeed(PXCMHandData.JointType.JOINT_MIDDLE_JT2, PXCMHandData.JointSpeedType.JOINT_SPEED_ABSOLUTE, time);
                handConfig.EnableJointSpeed(PXCMHandData.JointType.JOINT_MIDDLE_TIP, PXCMHandData.JointSpeedType.JOINT_SPEED_ABSOLUTE, time);
                handConfig.EnableJointSpeed(PXCMHandData.JointType.JOINT_PINKY_BASE, PXCMHandData.JointSpeedType.JOINT_SPEED_ABSOLUTE, time);
                handConfig.EnableJointSpeed(PXCMHandData.JointType.JOINT_PINKY_JT1, PXCMHandData.JointSpeedType.JOINT_SPEED_ABSOLUTE, time);
                handConfig.EnableJointSpeed(PXCMHandData.JointType.JOINT_PINKY_JT2, PXCMHandData.JointSpeedType.JOINT_SPEED_ABSOLUTE, time);
                handConfig.EnableJointSpeed(PXCMHandData.JointType.JOINT_PINKY_TIP, PXCMHandData.JointSpeedType.JOINT_SPEED_ABSOLUTE, time);
                handConfig.EnableJointSpeed(PXCMHandData.JointType.JOINT_RING_BASE, PXCMHandData.JointSpeedType.JOINT_SPEED_ABSOLUTE, time);
                handConfig.EnableJointSpeed(PXCMHandData.JointType.JOINT_RING_JT1, PXCMHandData.JointSpeedType.JOINT_SPEED_ABSOLUTE, time);
                handConfig.EnableJointSpeed(PXCMHandData.JointType.JOINT_RING_JT2, PXCMHandData.JointSpeedType.JOINT_SPEED_ABSOLUTE, time);
                handConfig.EnableJointSpeed(PXCMHandData.JointType.JOINT_RING_TIP, PXCMHandData.JointSpeedType.JOINT_SPEED_ABSOLUTE, time);
                handConfig.EnableJointSpeed(PXCMHandData.JointType.JOINT_THUMB_BASE, PXCMHandData.JointSpeedType.JOINT_SPEED_ABSOLUTE, time);
                handConfig.EnableJointSpeed(PXCMHandData.JointType.JOINT_THUMB_JT1, PXCMHandData.JointSpeedType.JOINT_SPEED_ABSOLUTE, time);
                handConfig.EnableJointSpeed(PXCMHandData.JointType.JOINT_THUMB_JT2, PXCMHandData.JointSpeedType.JOINT_SPEED_ABSOLUTE, time);
                handConfig.EnableJointSpeed(PXCMHandData.JointType.JOINT_THUMB_TIP, PXCMHandData.JointSpeedType.JOINT_SPEED_ABSOLUTE, time);
                handConfig.EnableJointSpeed(PXCMHandData.JointType.JOINT_WRIST, PXCMHandData.JointSpeedType.JOINT_SPEED_ABSOLUTE, time);

                handConfig.ApplyChanges(); // Changes only take effect when you call ApplyChanges

                // Create the hand data instance
                handData = handModule.CreateOutput();
                pxcmStatus status = sm.Init();

                //


                if (status.IsError())
                {

                    dev_state = Settings.Default.DEVICE_STATE_NOT_CONNECTED;
                    _logger.Error(this.GetType(), "Failed initializing RSDriver");
                    Dispose();
                    return false;
                }
                else
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
            bool state = true;
            
             // Main processing loop
             while (sm.AcquireFrame(true).IsSuccessful())
             {
                 try {
                    
                     // Retrieve current hand tracking results
                     handData.Update();

                     //*****************AL MOMENTO DISPONIBILE UNA SOLA MANO****************************
                     // Process hand tracking data
                     handData.QueryHandData(PXCMHandData.AccessOrderType.ACCESS_ORDER_BY_TIME, 0, out ihand);
                     // Resume next frame processing*/
                     sm.ReleaseFrame();
                     if (ihand == null)
                     {
                         if (state == true) {
                             Logging.Instance.Information(this.GetType(),"No hands");
                             state = false;
                         }
                     }
                     else
                     {
                         if (ihand.HasTrackedJoints())
                         {
                             if (state == false)
                             {
                                 Logging.Instance.Information(this.GetType(), "Hand acquired");
                                 state = true;
                             }

                             RSSkeleton sk = new RSSkeleton(ihand);
                             JointsFrameElement elem = sk.ToJointsFrameElement();

                             //ATTENZIONE COLLO DI BOTTIGLIA!!!!
                             bus[Settings.Default.DEV_OUTTYPE_SKELETON].receive(this, Settings.Default.DEV_OUTTYPE_SKELETON, JsonConvert.SerializeObject(elem));
                         }
                     }
                     

                }
                catch (Exception e)
                 {
                     Logging.Instance.Information(this.GetType(),"Errore processamento frame Realsense:"+e.ToString());
                 }

             }
              

        }

        public override void Dispose()
        {
            // Clean up
            Logging.Instance.Information(this.GetType(), "Realsense device connection closed.");
            handData.Dispose();
            sm.Dispose();
        }
    }

   

    public class RSSkeleton
    {
        public Joint JOINT_WRIST { get; set; }
        public Joint JOINT_MIDDLE_JT1 { get; set; }
        public Joint JOINT_CENTER { get; set; }
        public Joint JOINT_MIDDLE_JT2 { get; set; }
        public Joint JOINT_THUMB_BASE { get; set; }
        public Joint JOINT_MIDDLE_TIP { get; set; }
        public Joint JOINT_THUMB_JT1 { get; set; }
        public Joint JOINT_RING_BASE { get; set; }
        public Joint JOINT_THUMB_JT2 { get; set; }
        public Joint JOINT_RING_JT1 { get; set; }
        public Joint JOINT_THUMB_TIP { get; set; }
        public Joint JOINT_RING_JT2 { get; set; }
        public Joint JOINT_INDEX_BASE { get; set; }
        public Joint JOINT_RING_TIP { get; set; }
        public Joint JOINT_INDEX_JT1 { get; set; }
        public Joint JOINT_PINKY_BASE { get; set; }
        public Joint JOINT_INDEX_JT2 { get; set; }
        public Joint JOINT_PINKY_JT1 { get; set; }
        public Joint JOINT_INDEX_TIP { get; set; }
        public Joint JOINT_PINKY_JT2 { get; set; }
        public Joint JOINT_MIDDLE_BASE { get; set; }
        public Joint JOINT_PINKY_TIP { get; set; }

        public class Joint
        {
            public Int32 confidence { get; set; }
            public PXCMPoint3DF32 positionWorld { get; set; }
            public PXCMPoint3DF32 positionImage { get; set; }
            public PXCMPoint4DF32 localRotation { get; set; }
            public PXCMPoint4DF32 globalOrientation { get; set; }
            public PXCMPoint3DF32 speed { get; set; }

            public Joint(PXCMHandData.JointData jdata)
            {
                this.confidence = jdata.confidence;
                this.positionWorld = new PXCMPoint3DF32(jdata.positionWorld.x, jdata.positionWorld.y, jdata.positionWorld.z);
                this.positionImage = new PXCMPoint3DF32(jdata.positionImage.x, jdata.positionImage.y, jdata.positionImage.z);
                this.localRotation = new PXCMPoint4DF32(jdata.localRotation.x, jdata.localRotation.y, jdata.localRotation.z, jdata.localRotation.w);
                this.globalOrientation = new PXCMPoint4DF32(jdata.globalOrientation.x, jdata.globalOrientation.y, jdata.globalOrientation.z, jdata.globalOrientation.w);
                this.speed = new PXCMPoint3DF32(jdata.speed.x, jdata.speed.y, jdata.speed.z);
            }
        }

        public RSSkeleton(PXCMHandData.IHand ihand)
        {
            PXCMHandData.JointData jdata;
            //eseguo per tutti  i giunti
            ihand.QueryTrackedJoint(PXCMHandData.JointType.JOINT_WRIST, out jdata);
            this.JOINT_WRIST = new Joint(jdata);
            ihand.QueryTrackedJoint(PXCMHandData.JointType.JOINT_MIDDLE_JT1, out jdata);
            this.JOINT_MIDDLE_JT1 = new Joint(jdata);
            ihand.QueryTrackedJoint(PXCMHandData.JointType.JOINT_CENTER, out jdata);
            this.JOINT_CENTER = new Joint(jdata);
            ihand.QueryTrackedJoint(PXCMHandData.JointType.JOINT_MIDDLE_JT2, out jdata);
            this.JOINT_MIDDLE_JT2 = new Joint(jdata);
            ihand.QueryTrackedJoint(PXCMHandData.JointType.JOINT_THUMB_BASE, out jdata);
            this.JOINT_THUMB_BASE = new Joint(jdata);
            ihand.QueryTrackedJoint(PXCMHandData.JointType.JOINT_MIDDLE_TIP, out jdata);
            this.JOINT_MIDDLE_TIP = new Joint(jdata);
            ihand.QueryTrackedJoint(PXCMHandData.JointType.JOINT_THUMB_JT1, out jdata);
            this.JOINT_THUMB_JT1 = new Joint(jdata);
            ihand.QueryTrackedJoint(PXCMHandData.JointType.JOINT_RING_BASE, out jdata);
            this.JOINT_RING_BASE = new Joint(jdata);
            ihand.QueryTrackedJoint(PXCMHandData.JointType.JOINT_THUMB_JT2, out jdata);
            this.JOINT_THUMB_JT2 = new Joint(jdata);
            ihand.QueryTrackedJoint(PXCMHandData.JointType.JOINT_RING_JT1, out jdata);
            this.JOINT_RING_JT1 = new Joint(jdata);
            ihand.QueryTrackedJoint(PXCMHandData.JointType.JOINT_THUMB_TIP, out jdata);
            this.JOINT_THUMB_TIP = new Joint(jdata);
            ihand.QueryTrackedJoint(PXCMHandData.JointType.JOINT_RING_JT2, out jdata);
            this.JOINT_RING_JT2 = new Joint(jdata);
            ihand.QueryTrackedJoint(PXCMHandData.JointType.JOINT_INDEX_BASE, out jdata);
            this.JOINT_INDEX_BASE = new Joint(jdata);
            ihand.QueryTrackedJoint(PXCMHandData.JointType.JOINT_RING_TIP, out jdata);
            this.JOINT_RING_TIP = new Joint(jdata);
            ihand.QueryTrackedJoint(PXCMHandData.JointType.JOINT_INDEX_JT1, out jdata);
            this.JOINT_INDEX_JT1 = new Joint(jdata);
            ihand.QueryTrackedJoint(PXCMHandData.JointType.JOINT_PINKY_BASE, out jdata);
            this.JOINT_PINKY_BASE = new Joint(jdata);
            ihand.QueryTrackedJoint(PXCMHandData.JointType.JOINT_INDEX_JT2, out jdata);
            this.JOINT_INDEX_JT2 = new Joint(jdata);
            ihand.QueryTrackedJoint(PXCMHandData.JointType.JOINT_PINKY_JT1, out jdata);
            this.JOINT_PINKY_JT1 = new Joint(jdata);
            ihand.QueryTrackedJoint(PXCMHandData.JointType.JOINT_INDEX_TIP, out jdata);
            this.JOINT_INDEX_TIP = new Joint(jdata);
            ihand.QueryTrackedJoint(PXCMHandData.JointType.JOINT_PINKY_JT2, out jdata);
            this.JOINT_PINKY_JT2 = new Joint(jdata);
            ihand.QueryTrackedJoint(PXCMHandData.JointType.JOINT_MIDDLE_BASE, out jdata);
            this.JOINT_MIDDLE_BASE = new Joint(jdata);
            ihand.QueryTrackedJoint(PXCMHandData.JointType.JOINT_PINKY_TIP, out jdata);
            this.JOINT_PINKY_TIP = new Joint(jdata);
        }

        public JointsFrameElement ToJointsFrameElement()
        {
            JointsFrameElement frame = new JointsFrameElement();
            frame.timestamp = DateTime.UtcNow.Ticks;
            PropertyInfo[] props = this.GetType().GetProperties();
            frame.joints = new JointElement[props.Length];
           
            //set data and push
            for (int i = 0; i < props.Length; i++)
            {
                frame.joints[i] = new JointElement();
                Joint tmp = (Joint)props[i].GetValue(this);
                frame.joints[i].Name = props[i].Name;
                frame.joints[i].Pos2d = new Position2D(tmp.positionImage.x, tmp.positionImage.y);
                frame.joints[i].Pos3d = new Position3D(tmp.positionWorld.x, tmp.positionWorld.y, tmp.positionWorld.z);
                frame.joints[i].Velox = new Velocity(tmp.speed.x, tmp.speed.y, tmp.speed.z);
                frame.joints[i].Orient = new Orientation(tmp.localRotation.x, tmp.localRotation.y, tmp.localRotation.z, tmp.localRotation.w);
                frame.joints[i].State = "undefined";
                frame.joints[i].Confidence = tmp.confidence;
                }

            return frame;
        }
    }

}
