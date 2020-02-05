using Assets.RemoteHandsTracking.Data;

namespace Assets.RemoteHandsTracking
{
    public class HandsDataRecordedFrame
    {
        public HandData LeftHandRenderUpdate { get; set; }
        public HandData RightHandRenderUpdate { get; set; }
        public HandData LeftHandPhysicsUpdate { get; set; }
        public HandData RightHandPhysicsUpdate { get; set; }

        public bool HasAnyData => LeftHandRenderUpdate != null || RightHandRenderUpdate != null 
                                                               || LeftHandPhysicsUpdate != null || RightHandPhysicsUpdate != null;
    }
}