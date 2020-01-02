using System;

namespace Assets.RemoteHandsTracking.Data
{
    public class HandRelatedDataContainer
    {
        public HandRelatedData HandRelatedData { get; set; }
        public HandData HandData { get; set; }
        public SkeletonData SkeletonData { get; set; }
        public MeshData MeshData { get; set; }

        [Obsolete("Required for serialization")]
        public HandRelatedDataContainer() { }

        public HandRelatedDataContainer(HandRelatedData handRelatedData, HandData handData, SkeletonData skeletonData, MeshData meshData)
        {
            HandRelatedData = handRelatedData;
            HandData = handData;
            SkeletonData = skeletonData;
            MeshData = meshData;
        }

        public static HandRelatedDataContainer AsHandData(HandData handData)
        {
            return new HandRelatedDataContainer(HandRelatedData.Hand, handData, null, null);
        }

        public static HandRelatedDataContainer AsSkeletonData(SkeletonData skeletonData)
        {
            return new HandRelatedDataContainer(HandRelatedData.Skeleton, null, skeletonData, null);
        }

        public static HandRelatedDataContainer AsMeshData(MeshData meshData)
        {
            return new HandRelatedDataContainer(HandRelatedData.Mesh, null, null, meshData);
        }
    }
}
