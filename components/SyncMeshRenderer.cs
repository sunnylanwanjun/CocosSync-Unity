using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;

namespace CocosSync
{

    [Serializable]
    class SyncLightMapSetting
    {
        public string lightmapColor = "";
        public Vector4 uv;
        public string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [Serializable]
    class SyncMeshRendererData : SyncComponentData
    {
        public List<string> materilas = new List<string>();
        public string mesh = "";
        public string lightmapSetting;

        public override void Sync(Component c)
        {
            MeshRenderer comp = c as MeshRenderer;

            this.name = "cc.MeshRenderer";

            if (comp.lightmapIndex >= 0 && LightmapSettings.lightmaps.Length > comp.lightmapIndex)
            {
                var lightmapData = LightmapSettings.lightmaps.GetValue(comp.lightmapIndex) as LightmapData;

                var lightmapTex = SyncAssetData.GetAssetData<SyncTextureData>(lightmapData.lightmapColor);
                if (lightmapTex != null)
                {
                    var lightmapSetting = new SyncLightMapSetting();
                    lightmapSetting.lightmapColor = lightmapTex.uuid;
                    lightmapSetting.uv = new Vector4(comp.lightmapScaleOffset.z, comp.lightmapScaleOffset.w, comp.lightmapScaleOffset.x, comp.lightmapScaleOffset.y);

                    this.lightmapSetting = lightmapSetting.GetData();
                }
            }

            foreach (var m in comp.sharedMaterials)
            {
                var mtl = SyncAssetData.GetAssetData<SyncMaterialData>(m, this);
                if (mtl != null)
                {
                    this.materilas.Add(mtl.uuid);
                }
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