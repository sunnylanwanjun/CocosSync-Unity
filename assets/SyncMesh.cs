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
        public List<float> uv1 = new List<float>();
        public List<float> normals = new List<float>();
        public List<float> colors = new List<float>();
        public List<float> boneWeights = new List<float>();
        public List<float> joints = new List<float>();
        public List<int> indices = new List<int>();
    }

    [Serializable]
    class SyncMeshDataDetail
    {
        public List<SyncSubMeshData> subMeshes = new List<SyncSubMeshData>();
    }

    [Serializable]
    class SyncMeshData : SyncAssetData
    {
        public string meshName;
        public Vector3 min;
        public Vector3 max;

        private Mesh mesh;
        private int submeshStart;
        private int submeshCount;

        public override void Sync(UnityEngine.Object obj, object param1 = null)
        {
            Sync(obj, 0, 10000);
        }

        public void Sync(UnityEngine.Object obj, int start, int count)
        {
            name = "cc.Mesh";

            mesh = obj as Mesh;
            meshName = mesh.name;

            min = mesh.bounds.min;
            max = mesh.bounds.max;

            submeshStart = start;
            submeshCount = count;
        }

        public override string GetData()
        {
            return JsonUtility.ToJson(this);
        }

        public override string GetDetailData()
        {
            SyncMeshDataDetail data = new SyncMeshDataDetail();
            List<SyncSubMeshData> subMeshes = data.subMeshes;

            var vertices = new List<Vector3>();
            mesh.GetVertices(vertices);

            var normals = new List<Vector3>();
            mesh.GetNormals(normals);

            var colors = new List<Color>();
            mesh.GetColors(colors);

            var weights = new List<BoneWeight>();
            mesh.GetBoneWeights(weights);

            var uvs = new List<Vector2>();
            mesh.GetUVs(0, uvs);

            var uvs1 = new List<Vector2>();
            mesh.GetUVs(1, uvs1);

            int end = Math.Min(submeshStart + submeshCount, mesh.subMeshCount);

            for (var mi = submeshStart; mi < end; mi++)
            {
                var sm = mesh.GetSubMesh(mi);

                var smd = new SyncSubMeshData();
                subMeshes.Add(smd);

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

                        smd.joints.Add(weight.boneIndex0);
                        smd.joints.Add(weight.boneIndex1);
                        smd.joints.Add(weight.boneIndex2);
                        smd.joints.Add(weight.boneIndex3);
                    }

                    if (uvs.Count != 0)
                    {
                        var uv = uvs[sm.firstVertex + vi];
                        smd.uv.Add(uv.x);
                        smd.uv.Add(1 - uv.y);
                    }

                    if (uvs1.Count != 0)
                    {
                        var uv1 = uvs1[sm.firstVertex + vi];
                        smd.uv1.Add(uv1.x);
                        smd.uv1.Add(uv1.y);
                    }
                }

                var triangles = mesh.GetTriangles(mi);
                foreach (var v in triangles)
                {
                    smd.indices.Add(v);
                }
            }

            return JsonUtility.ToJson(data);
        }
    }
}