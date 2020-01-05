using System;
using Assets.RemoteHandsTracking.Data;
using Assets.RemoteHandsTracking.Extensions;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.RemoteHandsTracking.Customisations
{
    public class OVRHandMeshDataFeeder: MonoBehaviour
    {
        public UnityEvent HandMeshDataInitialized = new UnityEvent();

        public OVRMesh LeftHandMesh;
        public OVRMesh RightHandMesh;

        private OVRHandMeshReflection _leftHandMeshReflection;
        private OVRHandMeshReflection _rightHandMeshReflection;

        public void Start()
        {
            _leftHandMeshReflection = new OVRHandMeshReflection(LeftHandMesh);
            _rightHandMeshReflection = new OVRHandMeshReflection(RightHandMesh);
        }

        public void ProcessData(MeshData meshData)
        {
            var handMeshReflection = GetHandMeshReflection(meshData);
            if(handMeshReflection.IsInitProcessed) return;

            var mesh = InitializeMesh(meshData.MeshType, meshData.Mesh);
            handMeshReflection.Mesh.Set(mesh);

            handMeshReflection.IsInitProcessed = true;
            
            if (_leftHandMeshReflection.IsInitProcessed && _rightHandMeshReflection.IsInitProcessed)
            {
                HandMeshDataInitialized?.Invoke();
            }

            Debug.Log($"HandMesh({meshData.MeshType}) - initialized");

        }

        private OVRHandMeshReflection GetHandMeshReflection(MeshData meshData)
        {
            OVRHandMeshReflection reflection;
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

        private static Mesh InitializeMesh(OVRPlugin.MeshType meshType, OVRPlugin.Mesh ovrpMesh)
        {
            var mesh = new Mesh();

            //Same routine that initializes OVRMesh (without API call as data is provided)
            //var ovrpMesh = new OVRPlugin.Mesh();
            //if (OVRPlugin.GetMesh(meshType, out ovrpMesh))
            //{
                var vertices = new Vector3[ovrpMesh.NumVertices];
                for (int i = 0; i < ovrpMesh.NumVertices; ++i)
                {
                    vertices[i] = ovrpMesh.VertexPositions[i].FromFlippedZVector3f();
                }
                mesh.vertices = vertices;

                var uv = new Vector2[ovrpMesh.NumVertices];
                for (int i = 0; i < ovrpMesh.NumVertices; ++i)
                {
                    uv[i] = new Vector2(ovrpMesh.VertexUV0[i].x, -ovrpMesh.VertexUV0[i].y);
                }
                mesh.uv = uv;

                var triangles = new int[ovrpMesh.NumIndices];
                for (int i = 0; i < ovrpMesh.NumIndices; ++i)
                {
                    triangles[i] = ovrpMesh.Indices[ovrpMesh.NumIndices - i - 1];
                }
                mesh.triangles = triangles;

                var normals = new Vector3[ovrpMesh.NumVertices];
                for (int i = 0; i < ovrpMesh.NumVertices; ++i)
                {
                    normals[i] = ovrpMesh.VertexNormals[i].FromFlippedZVector3f();
                }
                mesh.normals = normals;

                var boneWeights = new BoneWeight[ovrpMesh.NumVertices];
                for (int i = 0; i < ovrpMesh.NumVertices; ++i)
                {
                    var currentBlendWeight = ovrpMesh.BlendWeights[i];
                    var currentBlendIndices = ovrpMesh.BlendIndices[i];

                    boneWeights[i].boneIndex0 = (int)currentBlendIndices.x;
                    boneWeights[i].weight0 = currentBlendWeight.x;
                    boneWeights[i].boneIndex1 = (int)currentBlendIndices.y;
                    boneWeights[i].weight1 = currentBlendWeight.y;
                    boneWeights[i].boneIndex2 = (int)currentBlendIndices.z;
                    boneWeights[i].weight2 = currentBlendWeight.z;
                    boneWeights[i].boneIndex3 = (int)currentBlendIndices.w;
                    boneWeights[i].weight3 = currentBlendWeight.w;
                }
                mesh.boneWeights = boneWeights;
            //}

            return mesh;
        }

        private class OVRHandMeshReflection
        {
            public OVRMesh HandMesh { get; }
            public TypeExtensions.FieldAccess<OVRMesh, Mesh> Mesh { get; }
            public bool IsInitProcessed { get; set; }


            public OVRHandMeshReflection(OVRMesh handMesh)
            {
                HandMesh = handMesh;
                Mesh = typeof(OVRMesh).CreateFieldAccess<OVRMesh, Mesh>( HandMesh, "_mesh");
            }
        }

    }
}
