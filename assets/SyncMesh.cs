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
            Sync(obj, 0, 10000);
        }

        public void Sync(UnityEngine.Object obj, int start, int count)
        {
            this.name = "cc.Mesh";

            Mesh m = obj as Mesh;
            this.meshName = m.name;

            var vertices = new List<Vector3>();
            m.GetVertices(vertices);

            var normals = new List<Vector3>();
            m.GetNormals(normals);

            var colors = new List<Color>();
            m.GetColors(colors);

            var weights = new List<BoneWeight>();
            m.GetBoneWeights(weights);

            var uvs = new List<Vector2>();
            m.GetUVs(0, uvs);

            int end = Math.Min(m.subMeshCount + count, m.subMeshCount);

            for (var mi = start; mi < end; mi++)
            {
                var sm = m.GetSubMesh(mi);

                var smd = new SyncSubMeshData();
                this.subMeshes.Add(smd);

                for (int vi = 0; vi < sm.vertexCount; vi++)
                {
                    if (vertices.Count != 0)
                    {
                        var v = vertices[sm.firstVertex + vi];
                        smd.vertices.Add(v.x);
                        smd.vertices.Add(v.y);
                        smd.vertices.Add(v.z);
                    }

                    if (normals.Count != 0)
                    {
                        var n = normals[sm.firstVertex + vi];
                        smd.normals.Add(n.x);
                        smd.normals.Add(n.y);
                        smd.normals.Add(n.z);
                    }

                    if (colors.Count != 0)
                    {
                        var c = colors[sm.firstVertex + vi];
                        smd.colors.Add(c.r);
                        smd.colors.Add(c.g);
                        smd.colors.Add(c.b);
                        smd.colors.Add(c.a);
                    }

                    if (weights.Count != 0)
                    {
                        var weight = weights[sm.firstVertex + vi];
                        smd.boneWeights.Add(weight.weight0);
                        smd.boneWeights.Add(weight.weight1);
                        smd.boneWeights.Add(weight.weight2);
                        smd.boneWeights.Add(weight.weight3);
                    }

                    if (uvs.Count != 0)
                    {
                        var uv = uvs[sm.firstVertex + vi];
                        smd.uv.Add(uv.x);
                        smd.uv.Add(1 - uv.y);
                    }
                }

                var triangles = m.GetTriangles(mi);
                foreach (var v in triangles)
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