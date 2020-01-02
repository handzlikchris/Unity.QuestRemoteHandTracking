using System;
using System.Collections;
using System.Reflection;
using Assets.RemoteHandsTracking.Data;
using Assets.RemoteHandsTracking.Extensions;
using OculusSampleFramework;
using UnityEngine;

namespace Assets.RemoteHandsTracking.Customisations
{
    public class OculusIntegrationExampleSkeletonDataFeeder : MonoBehaviour
    {
        public Hands Hands;

        private HandSkeletonReflection _leftHandSkeletonReflection;
        private HandSkeletonReflection _rightHandSkeletonReflection;

        private HandPhysicsReflection _leftHandPhysicsReflection;
        private HandPhysicsReflection _rightHandPhysicsReflection;

        private static int WaitNSecondsBetweenInitializationIfNotReady = 1;

        public void Start()
        {
            _leftHandSkeletonReflection = new HandSkeletonReflection(Hands.LeftHand.Skeleton);
            _rightHandSkeletonReflection = new HandSkeletonReflection(Hands.RightHand.Skeleton);

            _leftHandPhysicsReflection = new HandPhysicsReflection(Hands.LeftHand.Physics);
            _rightHandPhysicsReflection = new HandPhysicsReflection(Hands.RightHand.Physics);
        }

        public void ProcessData(SkeletonData skeletonData)
        {
            StartCoroutine(InitializeSkeletonOnSkeletonScript(skeletonData));
            StartCoroutine(InitializeSkeletonOnPhysicsScript(skeletonData));
        }

        private IEnumerator InitializeSkeletonOnSkeletonScript(SkeletonData skeletonData)
        {
            var handSkeletonReflection = GetHandSkeletonReflection(skeletonData);
            var skeleton = skeletonData.Skeleton;

            bool initSkeletonSuccess = false;
            while (!initSkeletonSuccess)
            {
                if (handSkeletonReflection.HandSkeleton.InitializeSkeleton(ref skeleton))
                {
                    initSkeletonSuccess = true;
                    yield return null;
                }
                
                yield return new WaitForSeconds(WaitNSecondsBetweenInitializationIfNotReady);
            }
            while (!handSkeletonReflection.AttacheBonesToMesh())
            {
                yield return null;
            }

            handSkeletonReflection.SetIsInitialized(true);
            Debug.Log($"Skeleton({skeletonData.SkeletonType}) - initialized");

            yield break;
        }

        private IEnumerator InitializeSkeletonOnPhysicsScript(SkeletonData skeletonData)
        {
            var handPhysicsReflection = GetHandPhysicsReflection(skeletonData);

            bool success = false;
            while (!success)
            {
                var skeleton = skeletonData.Skeleton;
                success = handPhysicsReflection.HandPhysics.InitializePhysics(ref skeleton);

                yield return null;
            }
            handPhysicsReflection.SetIsInitialized(true); ;
        }

        private HandSkeletonReflection GetHandSkeletonReflection(SkeletonData skeletonData)
        {
            HandSkeletonReflection reflection;
            switch (skeletonData.SkeletonType)
            {
                case OVRPlugin.SkeletonType.HandLeft:
                    reflection = _leftHandSkeletonReflection;
                    break;
                case OVRPlugin.SkeletonType.HandRight:
                    reflection = _rightHandSkeletonReflection;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            return reflection;
        }

        private HandPhysicsReflection GetHandPhysicsReflection(SkeletonData skeletonData)
        {
            HandPhysicsReflection reflection;
            switch (skeletonData.SkeletonType)
            {
                case OVRPlugin.SkeletonType.HandLeft:
                    reflection = _leftHandPhysicsReflection;
                    break;
                case OVRPlugin.SkeletonType.HandRight:
                    reflection = _rightHandPhysicsReflection;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            return reflection;
        }

        private class HandPhysicsReflection
        {
            public HandPhysics HandPhysics { get; }
            private Action<HandPhysics, bool> SetIsInitializedFunc { get; }

            public HandPhysicsReflection(HandPhysics handPhysics)
            {
                HandPhysics = handPhysics;
                SetIsInitializedFunc = typeof(HandPhysics).CreateSetFieldDelegate<HandPhysics, bool>("_isInitialized");

            }

            public void SetIsInitialized(bool isInitialized) => SetIsInitializedFunc(HandPhysics, isInitialized);
        }

        private class HandSkeletonReflection
        {
            public HandSkeleton HandSkeleton { get; }
            private Func<bool> AttacheBonesToMeshFunc { get; }
            private Action<HandSkeleton, bool> SetIsInitializedFunc { get; }

            public HandSkeletonReflection(HandSkeleton handSkeleton)
            {
                HandSkeleton = handSkeleton;

                var attachBonesToMeshMethod = typeof(HandSkeleton)
                    .GetMethod("AttacheBonesToMesh", BindingFlags.Instance | BindingFlags.NonPublic);
                AttacheBonesToMeshFunc = (Func<bool>)
                    Delegate.CreateDelegate(typeof(Func<bool>), handSkeleton, attachBonesToMeshMethod);

                SetIsInitializedFunc = typeof(HandSkeleton).CreateSetFieldDelegate<HandSkeleton, bool>("_isInitialized");

            }

            public bool AttacheBonesToMesh() => AttacheBonesToMeshFunc();
            public void SetIsInitialized(bool isInitialized) => SetIsInitializedFunc(HandSkeleton, isInitialized);
        }
    }
}