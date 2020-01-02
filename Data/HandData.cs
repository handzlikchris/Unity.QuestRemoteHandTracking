using System;

namespace Assets.RemoteHandsTracking.Data
{
    public class HandData
    {
        public OVRPlugin.Step Step { get; set; }
        public OVRPlugin.Hand Hand { get; set; }
        public OVRPlugin.HandState HandState { get; set; }

        public HandData(OVRPlugin.Step step, OVRPlugin.Hand hand, OVRPlugin.HandState handState)
        {
            Step = step;
            Hand = hand;
            HandState = handState;
        }

        [Obsolete("Required for serialization")]
        public HandData() { }
    }
}