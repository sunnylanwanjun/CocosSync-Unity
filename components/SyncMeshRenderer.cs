using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;

namespace CocosSync
{
    [Serializable]
    class SyncMeshRendererData : SyncRendererData
    {
        public string mesh = "";

        public override void Sync(Component c)
        {
            base.Sync(c);

            name = "cc.MeshRenderer";
            var meshRenderer = c as MeshRenderer;

            var filter = comp.GetComponent<MeshFilter>();
            if (filter && filter.sharedMesh)
            {
                if (filter.sharedMesh.name.StartsWith("Combined Mesh"))
                {
                    if (meshRenderer)
                    {
                        var path = "CombinedMesh/" + filter.name + "_" + (meshRenderer.subMeshStartIndex) + "_" + (meshRenderer.subMeshStartIndex + meshRenderer.sharedMaterials.Length - 1);

                        var asset = new SyncMeshData();
                        asset.uuid = path;
                        asset.path = path;

                        asset.shouldCheckSrc = false;

                        asset.Sync(filter.sharedMesh, meshRenderer.subMeshStartIndex, meshRenderer.sharedMaterials.Length);

                        SyncAssetData.AddAssetData(asset);
                    }
                }
                else
                {
                    var meshData = SyncAssetData.GetAssetData<SyncMeshData>(filter.sharedMesh);
                    if (meshData != null)
                    {
                        this.mesh = meshData.uuid;
                    }
                }
            }
        }

        public override string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }
}