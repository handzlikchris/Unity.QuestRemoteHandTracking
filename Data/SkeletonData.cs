using System;

namespace Assets.RemoteHandsTracking.Data
{
    public class SkeletonData
    {
        public OVRPlugin.Skeleton Skeleton { get; set; }
        public OVRPlugin.SkeletonType SkeletonType { get; set; }

        public SkeletonData(OVRPlugin.Skeleton skeleton, OVRPlugin.SkeletonType skeletonType)
        {
            Skeleton = skeleton;
            SkeletonType = skeletonType;
        }


        [Obsolete("Required for serialization")]
        public SkeletonData() { }
    }
}