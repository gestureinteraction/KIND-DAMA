using System;
using System.Collections.Generic;
using System.Threading;
using Managers.Busses;
using Utilities.Logger;
using Newtonsoft.Json;
using GestureInteractionServer.Properties;
using Models;
using Utilities;

namespace Managers.DevicesDriver
{
    public class DummyDriver: DeviceDriver
    {
        public DummyDriver(String id,  String name, String model, String producer, Dictionary<string, string[]> pars) : base(id, name, model, producer,pars)
        {
          
            //specifico quali bus sono attivabili
            databusparams[Settings.Default.DEV_OUTTYPE_SKELETON]=null;
            databusparams[Settings.Default.DEV_OUTTYPE_RGB] = null;
            databusparams[Settings.Default.DEV_OUTTYPE_DEPTH] = null;
            //riempio i parametri dei bus
            fillDataBusParamsKeys(pars);
        }

        public override bool start(String starttype)
        {

            
            bool started=base.start(starttype);

            if (started)
            {
                _logger.Information(this.GetType(), "Dummy device started ok", null);
                return true;
            }
            else
                return false;
        }



        public override String enableWriteOnBus(string key)
        {
            
            //se il bus è gia attivo oppure non è gestibile per qualche motivo
            String prova = base.enableWriteOnBus(key);
           
            if (prova == Settings.Default.BUS_ALREADY_ACTIVATED || prova == Settings.Default.BUS_NOT_MANAGED)
                return prova;


            if (key == Settings.Default.DEV_OUTTYPE_SKELETON)
            {

                //genero il thread per la scrittura dei dati dei giunti fittizi
                outthread[key] = new Thread(pushJointData);
                //paramlist[0] e paramlist[1] rappresentano ip e porta del bus
                //istanziare con reflection
            }
            else if (key == Settings.Default.DEV_OUTTYPE_RGB)
            {

                //genero il thread per la scrittura dei dati dei giunti fittizi
                outthread[key] = new Thread(pushRGBData);
                //paramlist[0] e paramlist[1] rappresentano ip e porta del bus
                //istanziare con reflection
            }
            else if (key == Settings.Default.DEV_OUTTYPE_DEPTH)
            {

                //genero il thread per la scrittura dei dati dei giunti fittizi
                outthread[key] = new Thread(pushDepthData);
                //paramlist[0] e paramlist[1] rappresentano ip e porta del bus
                //istanziare con reflection
            }
            //gestire altri tipi di dato se lo si desidera
            else return Settings.Default.BUS_NOT_MANAGED;

            bus[key] = (Bus) Activator.CreateInstance(TypeSolver.getTypeByName(databusparams[key][0])[0],databusparams[key][1], databusparams[key][2]);
            outthread[key].Start();
                
            
            return Settings.Default.BUS_ACTIVATED; 
        }


        public void pushDepthData()
        {
            // Main processing loop
            while (true)
            {
                //50 frames al secondo
                Thread.Sleep(33);
                bus[Settings.Default.DEV_OUTTYPE_DEPTH].receive(this, Settings.Default.DEV_OUTTYPE_DEPTH, JsonConvert.SerializeObject(new DepthFrameElement()));
            }

        }

        public void pushRGBData()
        {
            // Main processing loop
            while (true)
            {
                //50 frames al secondo
                Thread.Sleep(20);
                bus[Settings.Default.DEV_OUTTYPE_RGB].receive(this, Settings.Default.DEV_OUTTYPE_RGB, JsonConvert.SerializeObject(new RGBFrameElement()));
            }

        }

        public void pushJointData()
        {
            double i = 0;

            // Main processing loop
            while (true)
            {
                //50 frames al secondo
                Thread.Sleep(33);
                i += 0.1;
                JointElement AnkleLeft = new JointElement { Name = "AnkleLeft", Pos3d = new Position3D(-0.074160732328891754 +i, -0.68381601572036743 + i, 0.48994305729866028 + i), Pos2d = new Position2D(212 + i, 665 + i), Confidence = -1, State = "undefined", Velox = new Velocity(0.0, 0.0, 0.0), Orient = new Orientation(0.0, 0.0, 0.0, 1.0) };
                JointElement AnkleRight = new JointElement { Name = "AnkleRight", Pos3d = new Position3D(-0.074160732328891754 + i, -0.68381601572036743, 0.48994305729866028), Pos2d = new Position2D(403 + i, 596 + i), Confidence = -1, State = "undefined", Velox = new Velocity(0.0, 0.0, 0.0), Orient = new Orientation(0.0, 0.0, 0.0, 1.0) };
                JointElement ElbowLeft = new JointElement { Name = "ElbowLeft", Pos3d = new Position3D(-0.074160732328891754 + i, -0.68381601572036743, 0.48994305729866028), Pos2d = new Position2D(195 + i, 336 + i), Confidence = -1, State = "undefined", Velox = new Velocity(0.0, 0.0, 0.0), Orient = new Orientation(0.0, 0.0, 0.0, 1.0) };
                JointElement ElbowRight = new JointElement { Name = "ElbowRight", Pos3d = new Position3D(-0.074160732328891754 + i, -0.68381601572036743, 0.48994305729866028), Pos2d = new Position2D(391 + i, 337 + i), Confidence = -1, State = "undefined", Velox = new Velocity(0.0, 0.0, 0.0), Orient = new Orientation(0.0, 0.0, 0.0, 1.0) };
                JointElement FootLeft = new JointElement { Name = "FootLeft", Pos3d = new Position3D(-0.074160732328891754 + i, -0.68381601572036743, 0.48994305729866028), Pos2d = new Position2D(101 + i, 1658 + i), Confidence = -1, State = "undefined", Velox = new Velocity(0.0, 0.0, 0.0), Orient = new Orientation(0.0, 0.0, 0.0, 1.0) };
                JointElement FootRight = new JointElement { Name = "FootRight ", Pos3d = new Position3D(-0.074160732328891754 + i, -0.68381601572036743, 0.48994305729866028), Pos2d = new Position2D(581 + i, 1115 + i), Confidence = -1, State = "undefined", Velox = new Velocity(0.0, 0.0, 0.0), Orient = new Orientation(0.0, 0.0, 0.0, 1.0) };
                JointElement HandLeft = new JointElement { Name = "HandLeft", Pos3d = new Position3D(-0.074160732328891754 + i, -0.68381601572036743, 0.48994305729866028), Pos2d = new Position2D(163 + i, 398 + i), Confidence = -1, State = "undefined", Velox = new Velocity(0.0, 0.0, 0.0), Orient = new Orientation(0.0, 0.0, 0.0, 1.0) };
                JointElement HandRight = new JointElement { Name = "HandRight", Pos3d = new Position3D(-0.074160732328891754 + i, -0.68381601572036743, 0.48994305729866028), Pos2d = new Position2D(450 + i, 397 + i), Confidence = -1, State = "undefined", Velox = new Velocity(0.0, 0.0, 0.0), Orient = new Orientation(0.0, 0.0, 0.0, 1.0) };
                JointElement HandTipLeft = new JointElement { Name = "HandTipLeft", Pos3d = new Position3D(-0.074160732328891754 + i, -0.68381601572036743, 0.48994305729866028), Pos2d = new Position2D(147 + i, 426 + i), Confidence = -1, State = "undefined", Velox = new Velocity(0.0, 0.0, 0.0), Orient = new Orientation(0.0, 0.0, 0.0, 1.0) };
                JointElement HandTipRight = new JointElement { Name = "HandTipRight", Pos3d = new Position3D(-0.074160732328891754 + i, -0.68381601572036743, 0.48994305729866028), Pos2d = new Position2D(483 + i, 421 + i), Confidence = -1, State = "undefined", Velox = new Velocity(0.0, 0.0, 0.0), Orient = new Orientation(0.0, 0.0, 0.0, 1.0) };
                JointElement Head = new JointElement { Name = "Head", Pos3d = new Position3D(-0.074160732328891754, -0.68381601572036743 + i, 0.48994305729866028), Pos2d = new Position2D(262 + i, 375 + i), Confidence = -1, State = "undefined", Velox = new Velocity(0.0, 0.0, 0.0), Orient = new Orientation(0.0, 0.0, 0.0, 1.0) };
                JointElement HipLeft = new JointElement { Name = "HipLeft", Pos3d = new Position3D(-0.074160732328891754 + i, -0.68381601572036743, 0.48994305729866028), Pos2d = new Position2D(292 + i, 193 + i), Confidence = -1, State = "undefined", Velox = new Velocity(0.0, 0.0, 0.0), Orient = new Orientation(0.0, 0.0, 0.0, 1.0) };
                JointElement HipRight = new JointElement { Name = "HipRight", Pos3d = new Position3D(-0.074160732328891754 + i, -0.68381601572036743, 0.48994305729866028), Pos2d = new Position2D(307 + i, 375 + i), Confidence = -1, State = "undefined", Velox = new Velocity(0.0, 0.0, 0.0), Orient = new Orientation(0.0, 0.0, 0.0, 1.0) };
                JointElement KneeLeft = new JointElement { Name = "KneeLeft", Pos3d = new Position3D(-0.074160732328891754 + i, -0.68381601572036743, 0.48994305729866028), Pos2d = new Position2D(247 + i, 471 + i), Confidence = -1, State = "undefined", Velox = new Velocity(0.0, 0.0, 0.0), Orient = new Orientation(0.0, 0.0, 0.0, 1.0) };
                JointElement KneeRight = new JointElement { Name = "KneeRight", Pos3d = new Position3D(-0.074160732328891754 + i, -0.68381601572036743, 0.48994305729866028), Pos2d = new Position2D(343 + i, 454 + i), Confidence = -1, State = "undefined", Velox = new Velocity(0.0, 0.0, 0.0), Orient = new Orientation(0.0, 0.0, 0.0, 1.0) };
                JointElement Neck = new JointElement { Name = "Neck", Pos3d = new Position3D(-0.074160732328891754 + i, - 0.68381601572036743, 0.48994305729866028), Pos2d = new Position2D(288 + i, 243 + i), Confidence = -1, State = "undefined", Velox = new Velocity(0.0, 0.0, 0.0), Orient = new Orientation(0.0, 0.0, 0.0, 1.0) };
                JointElement ShoulderLeft = new JointElement { Name = "ShoulderLeft", Pos3d = new Position3D(-0.074160732328891754 + i, -0.68381601572036743, 0.48994305729866028), Pos2d = new Position2D(234 + i, 270 + i), Confidence = -1, State = "undefined", Velox = new Velocity(0.0, 0.0, 0.0), Orient = new Orientation(0.0, 0.0, 0.0, 1.0) };
                JointElement ShoulderRight = new JointElement { Name = "ShoulderRight", Pos3d = new Position3D(-0.074160732328891754 + i, -0.68381601572036743, 0.48994305729866028), Pos2d = new Position2D(344 + i, 276 + i), Confidence = -1, State = "undefined", Velox = new Velocity(0.0, 0.0, 0.0), Orient = new Orientation(0.0, 0.0, 0.0, 1.0) };
                JointElement SpineBase = new JointElement { Name = "SpineBase", Pos3d = new Position3D(-0.074160732328891754 + i, -0.68381601572036743, 0.48994305729866028), Pos2d = new Position2D(285 + i, 375 + i), Confidence = -1, State = "undefined", Velox = new Velocity(0.0, 0.0, 0.0), Orient = new Orientation(0.0, 0.0, 0.0, 1.0) };
                JointElement SpineMid = new JointElement { Name = "SpineMid", Pos3d = new Position3D(-0.074160732328891754 + i, -0.68381601572036743, 0.48994305729866028), Pos2d = new Position2D(286 + i, 309 + i), Confidence = -1, State = "undefined", Velox = new Velocity(0.0, 0.0, 0.0), Orient = new Orientation(0.0, 0.0, 0.0, 1.0) };
                JointElement SpineShoulder = new JointElement { Name = "SpineShoulder", Pos3d = new Position3D(-0.074160732328891754 + i, -0.68381601572036743, 0.48994305729866028), Pos2d = new Position2D(288 + i, 260 + i), Confidence = -1, State = "undefined", Velox = new Velocity(0.0, 0.0, 0.0), Orient = new Orientation(0.0, 0.0, 0.0, 1.0) };
                JointElement ThumbLeft = new JointElement { Name = "ThumbLeft", Pos3d = new Position3D(-0.074160732328891754 + i, -0.68381601572036743, 0.48994305729866028), Pos2d = new Position2D(185 + i, 427 + i), Confidence = -1, State = "undefined", Velox = new Velocity(0.0, 0.0, 0.0), Orient = new Orientation(0.0, 0.0, 0.0, 1.0) };
                JointElement ThumbRight = new JointElement { Name = "ThumbRight", Pos3d = new Position3D(-0.074160732328891754 + i, -0.68381601572036743, 0.48994305729866028), Pos2d = new Position2D(439 + i, 416 + i), Confidence = -1, State = "undefined", Velox = new Velocity(0.0, 0.0, 0.0), Orient = new Orientation(0.0, 0.0, 0.0, 1.0) };
                JointElement WristLeft = new JointElement { Name = "WristLeft", Pos3d = new Position3D(-0.074160732328891754 + i, -0.68381601572036743, 0.48994305729866028), Pos2d = new Position2D(172 + i, 385 + i), Confidence = -1, State = "undefined", Velox = new Velocity(0.0, 0.0, 0.0), Orient = new Orientation(0.0, 0.0, 0.0, 1.0) };
                JointElement WristRight = new JointElement { Name = "WristRight", Pos3d = new Position3D(-0.074160732328891754 + i, -0.68381601572036743, 0.48994305729866028), Pos2d = new Position2D(439 + i, 384 + i), Confidence = -1, State = "undefined", Velox = new Velocity(0.0, 0.0, 0.0), Orient = new Orientation(0.0, 0.0, 0.0, 1.0) };
				bus[Settings.Default.DEV_OUTTYPE_SKELETON].receive(this, Settings.Default.DEV_OUTTYPE_SKELETON, JsonConvert.SerializeObject(new JointsFrameElement { joints = new JointElement[] { AnkleLeft, AnkleRight, ElbowLeft, ElbowRight, FootLeft, FootRight, HandLeft, HandRight, HandTipLeft, HandTipRight, Head, HipLeft, HipRight, KneeLeft, KneeRight, Neck, ShoulderLeft, ShoulderRight, SpineBase, SpineMid, SpineShoulder, ThumbLeft, ThumbRight, WristLeft, WristRight }, timestamp = DateTime.UtcNow.Ticks }));
             }

        }
    }
}
