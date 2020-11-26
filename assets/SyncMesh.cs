using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace CocosSync
{

    [Serializable]
    class SyncSubMeshData
    {
        public List<float> vertices = new List<float>();
        public List<float> uv = new List<float>();
        public List<float> normals = new List<float>();
        public List<float> colors = new List<float>();
        public List<float> boneWeights = new List<float>();
        public List<int> indices = new List<int>();
    }

    [Serializable]
    class SyncMeshData : SyncAssetData
    {
        public string meshName;

        public List<SyncSubMeshData> subMeshes = new List<SyncSubMeshData>();
        public Vector3 min;
        public Vector3 max;

        public override void Sync(UnityEngine.Object obj)
        {
            this.name = "cc.Mesh";

            Mesh m = obj as Mesh;
            this.meshName = m.name;

            for (var mi = 0; mi < m.subMeshCount; mi++)
            {
                var sm = m.GetSubmesh(mi);

                var smd = new SyncSubMeshData();
                this.subMeshes.Add(smd);

                foreach (var v in sm.vertices)
                {
                    smd.vertices.Add(v.x);
                    smd.vertices.Add(v.y);
                    smd.vertices.Add(v.z);
                }
                foreach (var v in sm.uv)
                {
                    smd.uv.Add(v.x);
                    smd.uv.Add(1 - v.y);
                }
                foreach (var v in sm.normals)
                {
                    smd.normals.Add(v.x);
                    smd.normals.Add(v.y);
                    smd.normals.Add(v.z);
                }
                foreach (var v in sm.colors)
                {
                    smd.colors.Add(v.r);
                    smd.colors.Add(v.g);
                    smd.colors.Add(v.b);
                    smd.colors.Add(v.a);
                }
                foreach (var v in sm.boneWeights)
                {
                    smd.boneWeights.Add(v.weight0);
                    smd.boneWeights.Add(v.weight1);
                    smd.boneWeights.Add(v.weight2);
                    smd.boneWeights.Add(v.weight3);
                }
                foreach (var v in sm.triangles)
                {
                    smd.indices.Add(v);
                }
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