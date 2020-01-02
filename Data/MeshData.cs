using System;

namespace Assets.RemoteHandsTracking.Data
{
    public class MeshData
    {
        public OVRPlugin.MeshType MeshType { get; set; }
        public OVRPlugin.Mesh Mesh { get; set; }

        public MeshData(OVRPlugin.MeshType meshType, OVRPlugin.Mesh mesh)
        {
            MeshType = meshType;
            Mesh = mesh;
        }

        [Obsolete("Required for serialization")]
        public MeshData() { }
    }
}