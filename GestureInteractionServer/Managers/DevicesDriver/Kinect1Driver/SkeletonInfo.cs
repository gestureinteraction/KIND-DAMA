extern alias Kinect1;
using Kinect1.Microsoft.Kinect;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Managers.DevicesDriver.Kinect1DriverSupport
{
    public class SkeletonInfo : AbstractInfo
    {
        public class DepthSpaceJoint
        {
            public double X { get; set; }
            public double Y { get; set; }

            public override bool Equals(System.Object obj)
            {
                // If parameter is null return false.
                if (obj == null)
                {
                    return false;
                }

                // If parameter cannot be cast to Point return false.
                DepthSpaceJoint p = obj as DepthSpaceJoint;
                if ((System.Object)p == null)
                {
                    return false;
                }

                // Return true if the fields match:
                return (this.X == p.X) && (this.Y == p.Y);
            }

            public bool Equals(DepthSpaceJoint p)
            {
                // If parameter is null return false:
                if ((object)p == null)
                {
                    return false;
                }

                // Return true if the fields match:
                return (this.X == p.X) && (this.Y == p.Y);
            }

            public override int GetHashCode()
            {
                // Constant because equals tests mutable member.
                // This will give poor hash performance, but will prevent bugs.
                return 0;
            }
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
            public enum HandState
            {
                Closed,
                Lasso,
                NotTracked,
                Open,
                Unknown
            }

            [JsonProperty(PropertyName = "HandState")]
            [JsonConverter(typeof(StringEnumConverter))]
            public HandState hs { get; set; }

            public HandJoint(Joint j, HandState hs)
            {
                this.Name = j.Name;
                this.X = j.X;
                this.Y = j.Y;
                this.Z = j.Z;
                this.DepthSpace = j.DepthSpace;

                this.hs = hs;
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

        public void SetHandJointValue(string jointName, HandJoint value)
        {
            this.Joints.GetType().GetProperty(jointName).SetValue(this.Joints, value);
        }
    }
}
