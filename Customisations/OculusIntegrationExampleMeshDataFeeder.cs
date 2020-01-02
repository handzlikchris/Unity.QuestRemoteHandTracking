using System;
using System.Collections;
using System.Reflection;
using Assets.RemoteHandsTracking.Data;
using OculusSampleFramework;
using UnityEngine;

namespace Assets.RemoteHandsTracking.Customisations
{
    public class OculusIntegrationExampleMeshDataFeeder: MonoBehaviour
    {
        public Hands Hands;

        private HandMeshReflection _leftHandMeshReflection;
        private HandMeshReflection _rightHandMeshReflection;

        public void Start()
        {
            _leftHandMeshReflection = new HandMeshReflection(Hands.LeftHand.HandMesh);
            _rightHandMeshReflection = new HandMeshReflection(Hands.RightHand.HandMesh);
        }

        public void ProcessData(MeshData meshData)
        {
            StartCoroutine(InitializeHandMesh(meshData));
        }

        private IEnumerator InitializeHandMesh(MeshData meshData)
        {
            var handMeshReflection = GetHandMeshReflection(meshData);
            bool success = false;
            while (!success)
            {
                var mesh = meshData.Mesh;
                success = handMeshReflection.InitializeMesh(mesh);
                yield return null;
            }

            Debug.Log($"HandMesh({meshData.MeshType}) - initialized");
        }


        private HandMeshReflection GetHandMeshReflection(MeshData meshData)
        {
            HandMeshReflection reflection;
            switch (meshData.MeshType)
            {
                case OVRPlugin.MeshType.HandLeft:
                    reflection = _leftHandMeshReflection;
                    break;
                case OVRPlugin.MeshType.HandRight:
                    reflection = _rightHandMeshReflection;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            return reflection;
        }

        private class HandMeshReflection
        {
            public HandMesh HandMesh { get; }
            private MethodInfo InitializeMeshMethod { get; }

            public HandMeshReflection(HandMesh handMesh)
            {
                HandMesh = handMesh;

                InitializeMeshMethod = typeof(HandMesh).GetMethod("InitializeMesh", BindingFlags.Instance | BindingFlags.NonPublic);

            }

            public bool InitializeMesh(OVRPlugin.Mesh mesh)
            {
                return (bool) InitializeMeshMethod.Invoke(HandMesh, new object[] { mesh });
            }
        }
    }
}