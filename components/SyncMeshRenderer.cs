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
    class SyncMeshRendererProbe
    {
        public string probePath;
        public float weight;

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

        public bool casterShadow = false;
        public bool receiveShadow = true;

        public List<string> probes = new List<string>();

        public MeshRenderer meshRenderer;

        public override void Sync(Component c)
        {
            meshRenderer = c as MeshRenderer;
            Renderer comp = c as Renderer;

            this.name = "cc.MeshRenderer";

            this.casterShadow = comp.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off;
            this.receiveShadow = comp.receiveShadows;

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
                    var meshRenderer = comp as MeshRenderer;
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

            List<UnityEngine.Rendering.ReflectionProbeBlendInfo> probes = new List<UnityEngine.Rendering.ReflectionProbeBlendInfo>();
            comp.GetClosestReflectionProbes(probes);

            if (probes.Count != 0)
            {
                foreach (var probe in probes)
                {
                    var probeData = new SyncMeshRendererProbe();
                    probeData.probePath = Hierarchy.GetPath(probe.probe.transform, null);
                    probeData.weight = probe.weight;

                    this.probes.Add(probeData.GetData());
                }
            }
        }

        public override string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }
}