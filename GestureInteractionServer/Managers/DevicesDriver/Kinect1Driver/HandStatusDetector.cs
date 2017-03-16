extern alias Kinect1;
using System;
using Kinect1.Microsoft.Kinect;
using Kinect1.Microsoft.Kinect.Toolkit.Interaction;

/*
 * The following code is inspired from:
 * - http://dotneteers.net/blogs/vbandi/archive/2013/05/03/kinect-interactions-with-wpf-part-iii-demystifying-the-interaction-stream.aspx
 * - http://stackoverflow.com/a/22177286/738017
 */

namespace Managers.DevicesDriver.Kinect1DriverSupport.HandStatusDetector
{
    public enum HandStatus
    {
        Gripped,
        Released
    }

    public enum HandSide
    {
        Left,
        Right
    }

    public class HandStateInfo
    {
        public HandSide handSide { get; private set; }
        public HandStatus handStatus { get; private set;  }

        public HandStateInfo(InteractionHandType handType, InteractionHandEventType handState)
        {
            this.handSide = handType == InteractionHandType.Left ? HandSide.Left : HandSide.Right;
            this.handStatus = handState == InteractionHandEventType.Grip ? HandStatus.Gripped : HandStatus.Released;
        }
    }

    public class HandStatusDetector
    {
        public delegate void HandStatusDetectedHandler(object sender, HandStateInfo handStateInfo);
        public event HandStatusDetectedHandler HandStatusDetected;

        private KinectSensor kinectSensor;

        private InteractionStream interactionStream;
        private UserInfo[] userInfos;
        private Skeleton[] skeletonData;

        private class DummyInteractionClient : IInteractionClient
        {
            public InteractionInfo GetInteractionInfoAtLocation(
                int skeletonTrackingId,
                InteractionHandType handType,
                double x,
                double y)
            {
                var result = new InteractionInfo();
                result.IsGripTarget = true;
                result.IsPressTarget = true;
                result.PressAttractionPointX = 0.5;
                result.PressAttractionPointY = 0.5;
                result.PressTargetControlId = 1;

                return result;
            }
        }

        public HandStatusDetector(KinectSensor kinectSensor)
        {
            this.kinectSensor = kinectSensor;

            this.kinectSensor.DepthFrameReady += SensorOnDepthFrameReady;
            this.kinectSensor.SkeletonFrameReady += SensorOnSkeletonFrameReady;

            this.interactionStream = new InteractionStream(this.kinectSensor, new DummyInteractionClient());
            this.interactionStream.InteractionFrameReady += InteractionStreamOnInteractionFrameReady;
        }

        private void SensorOnDepthFrameReady(object sender, DepthImageFrameReadyEventArgs depthImageFrameReadyEventArgs)
        {
            using (DepthImageFrame depthFrame = depthImageFrameReadyEventArgs.OpenDepthImageFrame())
            {
                if (depthFrame == null)
                    return;

                try
                {
                    this.interactionStream.ProcessDepth(depthFrame.GetRawPixelData(), depthFrame.Timestamp);
                }
                catch (InvalidOperationException)
                {
                    // DepthFrame functions may throw when the sensor gets
                    // into a bad state.  Ignore the frame in that case.
                }
            }
        }

        private void SensorOnSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs skeletonFrameReadyEventArgs)
        {
            using (SkeletonFrame skeletonFrame = skeletonFrameReadyEventArgs.OpenSkeletonFrame())
            {
                if (skeletonFrame == null)
                    return;

                try
                {
                    this.skeletonData = new Skeleton[skeletonFrame.SkeletonArrayLength];

                    skeletonFrame.CopySkeletonDataTo(this.skeletonData);
                    var accelerometerReading = this.kinectSensor.AccelerometerGetCurrentReading();
                    this.interactionStream.ProcessSkeleton(this.skeletonData, accelerometerReading, skeletonFrame.Timestamp);
                }
                catch (InvalidOperationException)
                {
                    // SkeletonFrame functions may throw when the sensor gets
                    // into a bad state.  Ignore the frame in that case.
                }
            }
        }

        private void InteractionStreamOnInteractionFrameReady(object sender, InteractionFrameReadyEventArgs e)
        {
            using (InteractionFrame frame = e.OpenInteractionFrame())
            {
                if (frame != null)
                {
                    if (this.userInfos == null)
                    {
                        this.userInfos = new UserInfo[InteractionFrame.UserInfoArrayLength];
                    }

                    frame.CopyInteractionDataTo(this.userInfos);
                }
                else
                {
                    return;
                }
            }

            foreach (UserInfo userInfo in this.userInfos)
            {
                foreach (InteractionHandPointer handPointer in userInfo.HandPointers)
                {
                    InteractionHandEventType action = handPointer.HandEventType;

                    if (action != InteractionHandEventType.None)
                    {
                        InteractionHandType handSide = handPointer.HandType;

                        if (handSide != InteractionHandType.None)
                        {
                            // hand event
                            HandStatusDetected(this.interactionStream, new HandStateInfo(handSide, action));
                        }
                    }
                }
            }
        }

    }
}
