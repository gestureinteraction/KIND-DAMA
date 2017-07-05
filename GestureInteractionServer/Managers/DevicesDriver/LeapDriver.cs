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
using Leap;

namespace Managers.DevicesDriver
{

    public class LeapDriver : DeviceDriver
    {

        private Controller controller;

        public LeapDriver(String id, String name, String model, String producer,Dictionary<string,string[]> pars) : base(id, name, model, producer,pars)
        {
            //properties driver dependent
            databusparams[Settings.Default.DEV_OUTTYPE_SKELETON] = null;
           
            //riempio i parametri dei bus
            fillDataBusParamsKeys(pars);

        }

        public override bool start(String starttype){
            bool notConnected = base.start(starttype);

            if (notConnected)
            {
                
                controller = new Leap.Controller();
                Thread.Sleep(400);
                if (!controller.IsConnected)
                {

                    dev_state = Settings.Default.DEVICE_STATE_NOT_CONNECTED;
                    _logger.Error(this.GetType(), "Failed initializing LeapController");
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
            
            Frame fold = controller.Frame();
            Frame fnew;
            HandList hands;
            Hand firstHand;
            // Main processing loop
            while (true)
            {
                fnew = controller.Frame();
                //Console.WriteLine(fnew.Hands.Count);
                
                if (fnew != fold && fnew != null && fnew.Hands.Count>0)
                {
                    fold = fnew;
                    hands = fnew.Hands;
                    firstHand= hands[0];
                    LeapSkeleton sk = new LeapSkeleton(firstHand);
                    JointsFrameElement elem = sk.ToJointsFrameElement();
                    //ATTENZIONE COLLO DI BOTTIGLIA!!!!
                    bus[Settings.Default.DEV_OUTTYPE_SKELETON].receive(this, Settings.Default.DEV_OUTTYPE_SKELETON, JsonConvert.SerializeObject(elem));

                }
                Thread.Sleep(30);


            }
        }

        public override void Dispose()
        {
          
        }
    }

   

    public class LeapSkeleton
    {
        public Joint JOINT_THUMB  { get; set; }
        public Joint JOINT_INDEX  { get; set; }
        public Joint JOINT_MIDDLE { get; set; }
        public Joint JOINT_RING   { get; set; }
        public Joint JOINT_PINKY  { get; set; }
       



        public class Joint
        {
            public Int32 confidence { get; set; }
            public PXCMPoint3DF32 positionWorld { get; set; }
            public PXCMPoint3DF32 positionImage { get; set; }
            public PXCMPoint4DF32 localRotation { get; set; }
            public PXCMPoint4DF32 globalOrientation { get; set; }
            public PXCMPoint3DF32 speed { get; set; }

            public Joint(Finger f)
            {
                
                this.confidence = 1;
                this.positionWorld = new PXCMPoint3DF32(f.TipPosition.x, f.TipPosition.y, f.TipPosition.z);
                this.positionImage = new PXCMPoint3DF32(0,0,0);
                this.localRotation = new PXCMPoint4DF32(0,0,0,0);
                this.globalOrientation = new PXCMPoint4DF32(0,0,0,0);
                this.speed = new PXCMPoint3DF32(0,0,0);
            }
        }

        public LeapSkeleton(Hand hand)
        {

            FingerList fingers = hand.Fingers;
            //thumb
            Finger th = fingers[0];
            //index
            Finger ind = fingers[1];
            //middle
            Finger mid = fingers[2];
            //ring
            Finger rin = fingers[3];
            //pinky
            Finger pin = fingers[4];


            //eseguo per tutti  i giunti
            this.JOINT_THUMB = new Joint(th);
            this.JOINT_INDEX = new Joint(ind);
            this.JOINT_MIDDLE = new Joint(mid);
            this.JOINT_RING = new Joint(rin);
            this.JOINT_PINKY = new Joint(pin);
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
