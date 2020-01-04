using System;
using Assets.RemoteHandsTracking.Data;
using Assets.RemoteHandsTracking.Extensions;
using OculusSampleFramework;
using UnityEngine;

namespace Assets.RemoteHandsTracking.Customisations
{
    public class OculusIntegrationExampleHandsDataFeeder: HandsDataFeederBase
    {
        public Hands Hands;

        private HandReflection _leftHandReflection;
        private HandReflection _rightHandReflection;

        private Action _setScaledAlphaInLateUpdate;

        public void Start()
        {
            _leftHandReflection = new HandReflection(Hands.LeftHand);
            _rightHandReflection = new HandReflection(Hands.RightHand);
        }

        public override void ProcessData(HandData handData)
        {
            var handReflection = GetHandReflection(handData);
            ExecuteUpdatePose(handData, handReflection);
        }

        public void LateUpdate()
        {
            _setScaledAlphaInLateUpdate?.Invoke();
        }

        private void ExecuteUpdatePose(HandData handData, HandReflection handReflection)
        {
            var currentState = handData.HandState;

            var isTracked = (currentState.Status & OVRPlugin.HandStatus.HandTracked) == OVRPlugin.HandStatus.HandTracked;
            handReflection.SetIsTracked(isTracked);

            if (isTracked)
            {
                handReflection.SetHandConfidence(Hand.OVRPluginConfidenceToHand(currentState.HandConfidence));
            }
            else
            {
                handReflection.SetHandConfidence(Hand.HandTrackingConfidence.None);
            }

            //WARN: there is some issue with hand flickering most likely due to some timing issue when setting AlphaValue [being overriden by default process]
            //HACK: for now it may be easiest to always return 1f from 'ScaledAlpha' getter in 'Hand' class
            // Fade hand according to confidence.
            var scaledAlphaToApply = handReflection.HandConfidenceFader.NextAlphaValue(handReflection.Hand.HandConfidence);
            _setScaledAlphaInLateUpdate = () => //That update needs to happen in LateUpdate, otherwise it could be overriden by default process
            {
                handReflection.Hand.ScaledAlpha = scaledAlphaToApply;
                if (handReflection.Hand.HandMesh)
                {
                    handReflection.Hand.HandMesh.UpdatePose();
                }
            };
            // Update Pointer
            var pointer = handReflection.GetPointer();
            pointer.PointerPosition = currentState.PointerPose.Position.FromFlippedZVector3f();
            pointer.PointerOrientation = currentState.PointerPose.Orientation.FromFlippedZQuatf();
            pointer.PointerStatusValid = (currentState.Status & OVRPlugin.HandStatus.InputStateValid) ==
                                         OVRPlugin.HandStatus.InputStateValid;

            if (handReflection.Hand.Skeleton)
            {
                handReflection.Hand.Skeleton.UpdatePose(currentState);
            }

            if (handData.Step == OVRPlugin.Step.Physics && handReflection.Hand.Physics)
            {
                handReflection.Hand.Physics.UpdatePose();
            }
        }

        private HandReflection GetHandReflection(HandData handData)
        {
            HandReflection handReflection;
            switch (handData.Hand)
            {
                case OVRPlugin.Hand.HandLeft:
                    handReflection = _leftHandReflection;
                    break;
                case OVRPlugin.Hand.HandRight:
                    handReflection = _rightHandReflection;
                    break;
                case OVRPlugin.Hand.None:
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return handReflection;
        }

        private class HandReflection
        {
            private Action<Hand, bool> IsTrackedSetter { get; }
            private Action<Hand, Hand.HandTrackingConfidence> HandConfidenceSetter { get; }
            private Func<Hand, Hand.PointerState> PointerGetter { get; }
            public HandConfidenceFader HandConfidenceFader { get; }

            public Hand Hand { get; }


            public HandReflection(Hand hand)
            {
                Hand = hand;
                var handType = typeof(Hand);
                IsTrackedSetter = handType.CreateSetFieldDelegate<Hand, bool>("_isTracked");
                HandConfidenceSetter = handType.CreateSetFieldDelegate<Hand, Hand.HandTrackingConfidence>("_handConfidence");

                HandConfidenceFader = new HandConfidenceFader(40);
                PointerGetter = handType.CreateGetFieldDelegate<Hand, Hand.PointerState>("_pointer");
            }

            public void SetIsTracked(bool isTracked) => IsTrackedSetter(Hand, isTracked);
            public void SetHandConfidence(Hand.HandTrackingConfidence handConfidence) => HandConfidenceSetter(Hand, handConfidence);

            public Hand.PointerState GetPointer() => PointerGetter(Hand);
        }

        private class HandConfidenceFader
        {
            private static float MAX_ALPHA = 1.0F;
            private int _numberOfFrames;
            private float _minTrackedAlpha = 0.0f;
            private float _maxTrackedAlpha = MAX_ALPHA;
            private int _currentCount = 0;

            public HandConfidenceFader(int numberOfFramse)
            {
                _numberOfFrames = numberOfFramse;
            }

            public float NextAlphaValue(Hand.HandTrackingConfidence confidence)
            {
                var calculatedAlpha = 0.0f;
                switch (confidence)
                {
                    case Hand.HandTrackingConfidence.High:
                        _currentCount = Mathf.Min((_currentCount + 1), _numberOfFrames);
                        calculatedAlpha = Mathf.Clamp(_currentCount / (float)_numberOfFrames, _minTrackedAlpha, _maxTrackedAlpha);
                        break;
                    case Hand.HandTrackingConfidence.Low:
                        _currentCount = Mathf.Max((_currentCount - 1), 0);
                        calculatedAlpha = Mathf.Clamp(_currentCount / (float)_numberOfFrames, _minTrackedAlpha, _maxTrackedAlpha);
                        break;
                    default:
                        _currentCount = 0;
                        calculatedAlpha = 0.0f;
                        break;
                }
                return calculatedAlpha;
            }
        }
    }
}