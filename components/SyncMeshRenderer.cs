using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace CocosSync
{
    [Serializable]
    class SyncMeshRendererData : SyncComponentData
    {
        public List<string> materilas = new List<string>();
        public string mesh = "";

        public override void Sync(Component c)
        {
            MeshRenderer comp = c as MeshRenderer;

            this.name = "cc.MeshRenderer";

            foreach (var m in comp.sharedMaterials)
            {
                var uuid = SyncAssetData.GetAssetData<SyncMaterialData>(m);
                this.materilas.Add(uuid);
            }

            var filter = comp.GetComponent<MeshFilter>();
            if (filter && filter.sharedMesh)
            {
                if (filter.sharedMesh.name.StartsWith("Combined Mesh"))
                {
                    var path = "CombinedMesh/" + filter.name + "_" + (comp.subMeshStartIndex) + "_" + (comp.subMeshStartIndex + comp.sharedMaterials.Length - 1);

                    var asset = new SyncMeshData();
                    asset.uuid = path;
                    asset.path = path;

                    asset.shouldCheckSrc = false;

                    asset.Sync(filter.sharedMesh, comp.subMeshStartIndex, comp.sharedMaterials.Length);
                    CocosSyncTool.sceneData.assets.Add(asset.GetData());
                }
                else
                {
                    this.mesh = SyncAssetData.GetAssetData<SyncMeshData>(filter.sharedMesh);
                }
            }
        }

        public override string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }
}