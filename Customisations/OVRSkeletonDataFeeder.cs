using System;
using System.Collections.Generic;
using Assets.RemoteHandsTracking.Data;
using Assets.RemoteHandsTracking.Extensions;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.RemoteHandsTracking.Customisations
{
    public class OVRSkeletonDataFeeder: MonoBehaviour
    {
        public UnityEvent SkeletonDataInitialized = new UnityEvent(); 

        public OVRSkeleton LeftHandSkeleton;
        public OVRSkeleton RightHandSkeleton;

        private OVRSkeletonReflection _leftHandSkeletonReflection;
        private OVRSkeletonReflection _rightHandSkeletonReflection;

        public void Start()
        {
            _leftHandSkeletonReflection = new OVRSkeletonReflection(LeftHandSkeleton);
            _rightHandSkeletonReflection = new OVRSkeletonReflection(RightHandSkeleton);
        }

        public void ProcessData(SkeletonData skeletonData)
        {
            var handSkeletonReflection = GetHandSkeletonReflection(skeletonData.SkeletonType);
            if(handSkeletonReflection.IsInitProcessed) return;

            InitializeSkeleton(skeletonData.SkeletonType, skeletonData.Skeleton);

            handSkeletonReflection.IsInitProcessed = true;

            if (_leftHandSkeletonReflection.IsInitProcessed && _rightHandSkeletonReflection.IsInitProcessed)
            {
                SkeletonDataInitialized?.Invoke();
            }

            Debug.Log($"Skeleton({skeletonData.SkeletonType}) - initialized");
        }

        private void InitializeSkeleton(OVRPlugin.SkeletonType skeletonType, OVRPlugin.Skeleton skeleton)
        {
            var handSkeletonReflection = GetHandSkeletonReflection(skeletonType);

            //Same routine that initializes OVRSkeleton (without API call as data is provided)
            //var skeleton = new OVRPlugin.Skeleton();
            //if (OVRPlugin.GetSkeleton((OVRPlugin.SkeletonType)_skeletonType, out skeleton))
            //{
            var bonesGO = handSkeletonReflection.BonesGo.Get();
            if (!bonesGO)
            {
                bonesGO = new GameObject("Bones");
                bonesGO.transform.SetParent(handSkeletonReflection.Skeleton.transform, false);
                bonesGO.transform.localPosition = Vector3.zero;
                bonesGO.transform.localRotation = Quaternion.identity;

                handSkeletonReflection.BonesGo.Set(bonesGO);
            }
            
            var bindPosesGO = handSkeletonReflection.BindPosesGo.Get();
            if (!bindPosesGO)
            {
                bindPosesGO = new GameObject("BindPoses");
                bindPosesGO.transform.SetParent(handSkeletonReflection.Skeleton.transform, false);
                bindPosesGO.transform.localPosition = Vector3.zero;
                bindPosesGO.transform.localRotation = Quaternion.identity;

                handSkeletonReflection.BindPosesGo.Set(bindPosesGO);
            }

            var enablePhysicsCapsules = handSkeletonReflection.EnablePhysicsCapsules.Get();
            var capsulesGO = handSkeletonReflection.CapsulesGo.Get();
            if (enablePhysicsCapsules)
            {
                if (!capsulesGO)
                {
                    capsulesGO = new GameObject("Capsules");
                    capsulesGO.transform.SetParent(handSkeletonReflection.Skeleton.transform, false);
                    capsulesGO.transform.localPosition = Vector3.zero;
                    capsulesGO.transform.localRotation = Quaternion.identity;

                    handSkeletonReflection.CapsulesGo.Set(capsulesGO);
                }
            }

            var bones = new List<OVRBone>(new OVRBone[skeleton.NumBones]);
            handSkeletonReflection.Bones.Set(bones);
            handSkeletonReflection.BonesProperty.Set(bones.AsReadOnly());

            var _bindPoses = new List<OVRBone>(new OVRBone[skeleton.NumBones]);
            handSkeletonReflection.BindPoses.Set(_bindPoses);
            handSkeletonReflection.BindPosesProperty.Set(_bindPoses.AsReadOnly());

            // pre-populate bones list before attempting to apply bone hierarchy
            for (int i = 0; i < skeleton.NumBones; ++i)
            {
                var id = (OVRSkeleton.BoneId)skeleton.Bones[i].Id;
                short parentIdx = skeleton.Bones[i].ParentBoneIndex;
                Vector3 pos = skeleton.Bones[i].Pose.Position.FromFlippedZVector3f();
                Quaternion rot = skeleton.Bones[i].Pose.Orientation.FromFlippedZQuatf();

                var boneGO = new GameObject(id.ToString());
                boneGO.transform.localPosition = pos;
                boneGO.transform.localRotation = rot;
                bones[i] = new OVRBone(id, parentIdx, boneGO.transform);

                var bindPoseGO = new GameObject(id.ToString());
                bindPoseGO.transform.localPosition = pos;
                bindPoseGO.transform.localRotation = rot;
                _bindPoses[i] = new OVRBone(id, parentIdx, bindPoseGO.transform);
            }

            for (int i = 0; i < skeleton.NumBones; ++i)
            {
                if (((OVRPlugin.BoneId)skeleton.Bones[i].ParentBoneIndex) == OVRPlugin.BoneId.Invalid)
                {
                    bones[i].Transform.SetParent(bonesGO.transform, false);
                    _bindPoses[i].Transform.SetParent(bindPosesGO.transform, false);
                }
                else
                {
                    bones[i].Transform.SetParent(bones[bones[i].ParentBoneIndex].Transform, false);
                    _bindPoses[i].Transform.SetParent(_bindPoses[bones[i].ParentBoneIndex].Transform, false);
                }
            }

            if (enablePhysicsCapsules)
            {
                var capsules = new List<OVRBoneCapsule>(new OVRBoneCapsule[skeleton.NumBoneCapsules]);
                handSkeletonReflection.Capsules.Set(capsules);
                handSkeletonReflection.CapsulesProperty.Set(capsules.AsReadOnly());

                var bonesProperty = handSkeletonReflection.Bones.Get();
                for (int i = 0; i < skeleton.NumBoneCapsules; ++i)
                {
                    var capsule = skeleton.BoneCapsules[i];
                    Transform bone = bonesProperty[capsule.BoneIndex].Transform;

                    var capsuleRigidBodyGO = new GameObject((bonesProperty[capsule.BoneIndex].Id).ToString() + "_CapsuleRigidBody");
                    capsuleRigidBodyGO.transform.SetParent(capsulesGO.transform, false);
                    capsuleRigidBodyGO.transform.localPosition = bone.position;
                    capsuleRigidBodyGO.transform.localRotation = bone.rotation;

                    var capsuleRigidBody = capsuleRigidBodyGO.AddComponent<Rigidbody>();
                    capsuleRigidBody.mass = 1.0f;
                    capsuleRigidBody.isKinematic = true;
                    capsuleRigidBody.useGravity = false;
#if UNITY_2018_3_OR_NEWER
                    capsuleRigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
#else
				capsuleRigidBody.collisionDetectionMode = CollisionDetectionMode.Continuous;
#endif

                    var capsuleColliderGO = new GameObject((bonesProperty[capsule.BoneIndex].Id).ToString() + "_CapsuleCollider");
                    capsuleColliderGO.transform.SetParent(capsuleRigidBodyGO.transform, false);
                    var capsuleCollider = capsuleColliderGO.AddComponent<CapsuleCollider>();
                    var p0 = capsule.Points[0].FromFlippedZVector3f();
                    var p1 = capsule.Points[1].FromFlippedZVector3f();
                    var delta = p1 - p0;
                    var mag = delta.magnitude;
                    var rot = Quaternion.FromToRotation(capsuleRigidBodyGO.transform.localRotation * Vector3.right, delta);
                    capsuleCollider.radius = capsule.Radius;
                    capsuleCollider.height = mag + capsule.Radius * 2.0f;
                    capsuleCollider.isTrigger = false;
                    capsuleCollider.direction = 0;
                    capsuleColliderGO.transform.localPosition = p0;
                    capsuleColliderGO.transform.localRotation = rot;
                    capsuleCollider.center = Vector3.right * mag * 0.5f;

                    capsules[i] = new OVRBoneCapsule(capsule.BoneIndex, capsuleRigidBody, capsuleCollider);
                //}
                }

                handSkeletonReflection.IsInitialized.Set(true);
            }
        }

        private OVRSkeletonReflection GetHandSkeletonReflection(OVRPlugin.SkeletonType skeletonType)
        {
            OVRSkeletonReflection reflection;
            switch (skeletonType)
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


        private class OVRSkeletonReflection
        {
            private Type t;
            public OVRSkeleton Skeleton { get; }
            public TypeExtensions.FieldAccess<OVRSkeleton, GameObject> BonesGo { get; }
            public TypeExtensions.FieldAccess<OVRSkeleton, GameObject> BindPosesGo { get; }
            public TypeExtensions.FieldAccess<OVRSkeleton, GameObject> CapsulesGo { get; }
            public TypeExtensions.FieldAccess<OVRSkeleton, bool> EnablePhysicsCapsules { get; }
            public TypeExtensions.FieldAccess<OVRSkeleton, List<OVRBone>> Bones { get; }
            public TypeExtensions.FieldAccess<OVRSkeleton, IList<OVRBone>> BonesProperty { get; }
            public TypeExtensions.FieldAccess<OVRSkeleton, List<OVRBone>> BindPoses { get; }
            public TypeExtensions.FieldAccess<OVRSkeleton, IList<OVRBone>> BindPosesProperty { get; }
            public TypeExtensions.FieldAccess<OVRSkeleton, List<OVRBoneCapsule>> Capsules { get; }
            public TypeExtensions.FieldAccess<OVRSkeleton, IList<OVRBoneCapsule>> CapsulesProperty { get; }
            public TypeExtensions.FieldAccess<OVRSkeleton, bool> IsInitialized { get; }
            
            public bool IsInitProcessed { get; set; }
            
            public OVRSkeletonReflection(OVRSkeleton skeleton)
            {
                Skeleton = skeleton;
                t = typeof(OVRSkeleton);
                BonesGo = t.CreateFieldAccess<OVRSkeleton, GameObject>(Skeleton, "_bonesGO");
                BindPosesGo = t.CreateFieldAccess<OVRSkeleton, GameObject>(Skeleton, "_bindPosesGO");
                EnablePhysicsCapsules = t.CreateFieldAccess<OVRSkeleton, bool>(Skeleton, "_enablePhysicsCapsules");
                CapsulesGo = t.CreateFieldAccess<OVRSkeleton, GameObject>(Skeleton, "_capsulesGO");
                Bones = t.CreateFieldAccess<OVRSkeleton, List<OVRBone>>(Skeleton, "_bones");
                BonesProperty = t.CreateFieldAccess<OVRSkeleton, IList<OVRBone>>(Skeleton, "Bones", true);
                BindPoses = t.CreateFieldAccess<OVRSkeleton, List<OVRBone>>(Skeleton, "_bindPoses");
                BindPosesProperty = t.CreateFieldAccess<OVRSkeleton, IList<OVRBone>>(Skeleton, "BindPoses", true);
                Capsules = t.CreateFieldAccess<OVRSkeleton, List<OVRBoneCapsule>>(Skeleton, "_capsules");
                CapsulesProperty = t.CreateFieldAccess<OVRSkeleton, IList<OVRBoneCapsule>>(Skeleton, "Capsules", true);
                IsInitialized = t.CreateFieldAccess<OVRSkeleton, bool>(Skeleton, "_isInitialized");
            }
        }

    }
}
