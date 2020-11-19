using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace CocosSync
{

    [Serializable]
    class SyncMeshData : SyncAssetData
    {
        public string meshName;

        public List<float> vertices = new List<float>();
        public List<float> uv = new List<float>();
        public List<float> normals = new List<float>();
        public List<float> boneWeights = new List<float>();
        public List<int> indices = new List<int>();
        public Vector3 min;
        public Vector3 max;

        public override void Sync(UnityEngine.Object obj)
        {
            this.name = "cc.Mesh";

            Mesh m = obj as Mesh;
            this.meshName = m.name;

            foreach (var v in m.vertices)
            {
                this.vertices.Add(v.x);
                this.vertices.Add(v.y);
                this.vertices.Add(v.z);
            }
            foreach (var v in m.uv)
            {
                this.uv.Add(v.x);
                this.uv.Add(1 - v.y);
            }
            foreach (var v in m.normals)
            {
                this.normals.Add(v.x);
                this.normals.Add(v.y);
                this.normals.Add(v.z);
            }
            foreach (var v in m.boneWeights)
            {
                this.boneWeights.Add(v.weight0);
                this.boneWeights.Add(v.weight1);
                this.boneWeights.Add(v.weight2);
                this.boneWeights.Add(v.weight3);
            }
            foreach (var v in m.triangles)
            {
                this.indices.Add(v);
            }
            this.min = m.bounds.min;
            this.max = m.bounds.max;
        }

        public override string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }
}