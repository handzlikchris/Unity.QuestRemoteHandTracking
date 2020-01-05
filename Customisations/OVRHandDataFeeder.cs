using System;
using System.Reflection;
using Assets.RemoteHandsTracking.Data;
using Assets.RemoteHandsTracking.Extensions;
using UnityEngine;

namespace Assets.RemoteHandsTracking.Customisations
{
    public class OVRHandDataFeeder : MonoBehaviour
    {
        public OVRHand LeftHand;
        public OVRHand RightHand;

        public bool IsMeshDataSet { get; set; }
        public bool IsSkeletonDataSet { get; set; }

        private OVRHandReflection _leftHandReflection;
        private OVRHandReflection _rightHandReflection;

        private void Awake()
        {
            _leftHandReflection = new OVRHandReflection(LeftHand);
            _rightHandReflection = new OVRHandReflection(RightHand);

            InitHand(OVRPlugin.Hand.HandLeft);
            InitHand(OVRPlugin.Hand.HandRight);
        }

        public void InitHand(OVRPlugin.Hand hand)
        {
            var handReflection = GetHandReflection(hand);
            var _pointerPoseGO = new GameObject();
            handReflection.PointerPoseGo.Set(_pointerPoseGO);
            handReflection.PointerPose.Set(_pointerPoseGO.transform);
        }

        public void ProcessData(HandData handData)
        {
            if(IsMeshDataSet && IsSkeletonDataSet)
            {
                var handReflection = GetHandReflection(handData.Hand);

                if (!handReflection.IsMeshRendererInitializeAlreadyCalled)
                {
                    var ovrMeshRenderer = handReflection.Hand.GetComponent<OVRMeshRenderer>();
                    typeof(OVRMeshRenderer).GetMethod("Initialize", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ovrMeshRenderer, new object[0]);
                    handReflection.IsMeshRendererInitializeAlreadyCalled = true;
                }
                
                SetHandData(handData);
            }
        }

        private void LateUpdate()
        {
            ApplyIsInitializedOverride(_leftHandReflection);
            ApplyIsInitializedOverride(_rightHandReflection);
        }

        private void ApplyIsInitializedOverride(OVRHandReflection handReflection)
        {
            handReflection.IsInitialized.Set(handReflection.IsInitializedOverride);
        }

        private void SetHandData(HandData handData)
        {
            var handState = handData.HandState;
            var handReflection = GetHandReflection(handData.Hand);
            //if (OVRPlugin.GetHandState(step, (OVRPlugin.Hand)HandType, ref _handState))
            //{
            handReflection.HandState.Set(handState); //TODO: hand state is probably overwritten

            handReflection.IsTracked.Set((handState.Status & OVRPlugin.HandStatus.HandTracked) != 0);
            handReflection.IsSystemGestureInProgress.Set((handState.Status & OVRPlugin.HandStatus.SystemGestureInProgress) != 0);
            handReflection.IsPointerPoseValid.Set((handState.Status & OVRPlugin.HandStatus.InputStateValid) != 0);
            handReflection.PointerPose.Get().localPosition = handState.PointerPose.Position.FromFlippedZVector3f();
            handReflection.PointerPose.Get().localRotation = handState.PointerPose.Orientation.FromFlippedZQuatf();
            handReflection.HandScale.Set(handState.HandScale);
            handReflection.HandConfidence.Set((OVRHand.TrackingConfidence)handState.HandConfidence);

            handReflection.IsInitialized.Set(true);;
            _leftHandReflection.IsInitializedOverride = true;
            //}
            //else
            //{
            //_isInitialized = false;

            //}
        }


        private OVRHandReflection GetHandReflection(OVRPlugin.Hand hand)
        {
            OVRHandReflection handReflection;
            switch (hand)
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



        private class OVRHandReflection
        {
            public OVRHand Hand { get; }
            public TypeExtensions.FieldAccess<OVRHand, GameObject> PointerPoseGo { get; }
            public TypeExtensions.FieldAccess<OVRHand, Transform> PointerPose { get; }
            public TypeExtensions.FieldAccess<OVRHand, bool> IsInitialized { get; }
            public TypeExtensions.FieldAccess<OVRHand, bool> IsTracked { get; }
            public TypeExtensions.FieldAccess<OVRHand, bool> IsSystemGestureInProgress { get; }
            public TypeExtensions.FieldAccess<OVRHand, bool> IsPointerPoseValid { get; }
            public TypeExtensions.FieldAccess<OVRHand, float> HandScale { get; }
            public TypeExtensions.FieldAccess<OVRHand, OVRHand.TrackingConfidence> HandConfidence { get; }
            public TypeExtensions.FieldAccess<OVRHand, OVRPlugin.HandState> HandState { get; }
            public bool IsInitializedOverride { get; set; }
            public bool IsMeshRendererInitializeAlreadyCalled { get; set; }

            public OVRHandReflection(OVRHand hand)
            {
                Hand = hand;
                PointerPoseGo = typeof(OVRHand).CreateFieldAccess<OVRHand, GameObject>(Hand, "_pointerPoseGO");
                PointerPose = typeof(OVRHand).CreateFieldAccess<OVRHand, Transform>(Hand, "PointerPose", true);
                IsInitialized = typeof(OVRHand).CreateFieldAccess<OVRHand, bool>(Hand, "_isInitialized");
                IsTracked = typeof(OVRHand).CreateFieldAccess<OVRHand, bool>(Hand, "IsTracked", true);
                IsSystemGestureInProgress = typeof(OVRHand).CreateFieldAccess<OVRHand, bool>(Hand, "IsSystemGestureInProgress", true);
                IsPointerPoseValid = typeof(OVRHand).CreateFieldAccess<OVRHand, bool>(Hand, "IsPointerPoseValid", true);
                HandScale = typeof(OVRHand).CreateFieldAccess<OVRHand, float>(Hand, "HandScale", true);
                HandConfidence = typeof(OVRHand).CreateFieldAccess<OVRHand, OVRHand.TrackingConfidence>(Hand, "HandConfidence", true);
                HandState = typeof(OVRHand).CreateFieldAccess<OVRHand, OVRPlugin.HandState>(Hand, "_handState");
            }
        }

    }
}
