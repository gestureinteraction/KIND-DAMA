using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Managers.DevicesDriver.Kinect1DriverSupport
{
    public class HeatMapDataset
    {
        public class HeatMapData
        {
            public int x {get; private set;}
            public int y {get; private set;}
            public float value {get; private set;}

            public HeatMapData(SkeletonInfo.DepthSpaceJoint dsj)
            {
                this.value = 0;
                this.x = (int)dsj.X;
                this.y = (int)dsj.Y;
            }

            public void IncrementScore() {
                this.value += 1;
            }
        }

        private IDictionary<SkeletonInfo.DepthSpaceJoint, HeatMapData> leftHeathMap = new Dictionary<SkeletonInfo.DepthSpaceJoint, HeatMapData>();
        private IDictionary<SkeletonInfo.DepthSpaceJoint, HeatMapData> rightHeathMap = new Dictionary<SkeletonInfo.DepthSpaceJoint, HeatMapData>();

        public void AddData(SkeletonInfo.DepthSpaceJoint dsj, HandStatusDetector.HandSide handSide)
        {
            IDictionary<SkeletonInfo.DepthSpaceJoint, HeatMapData> dict;

            if (handSide == HandStatusDetector.HandSide.Left)
            {
                dict = this.leftHeathMap;
            }
            else
            {
                dict = this.rightHeathMap;
            }

            if (!dict.ContainsKey(dsj))
            {
                dict[dsj] = new HeatMapData(dsj);
            }
            dict[dsj].IncrementScore();
        }

        public HeatMapData[] GetLeftHeathMap()
        {
            return this.leftHeathMap.Values.ToArray();
        }

        public HeatMapData[] GetRightHeathMap()
        {
            return this.rightHeathMap.Values.ToArray();
        }

    }
}
