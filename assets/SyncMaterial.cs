using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace CocosSync
{
    [Serializable]
    class SyncShaderProperty
    {
        public String name;
        public String value;
        public int type;
    }

    [Serializable]
    class SyncPassState
    {
        public int cullMode;
    }

    public enum BlendMode
    {
        Opaque,
        Cutout,
        Fade,
        Transparent
    }

    public enum SmoothChannel
    {
        None,
        Albedo_A,
        Metallic_A,
    }

    [Serializable]
    class SyncMaterialData : SyncAssetData
    {
        public String shaderUuid = "";
        public List<SyncShaderProperty> properties = new List<SyncShaderProperty>();
        public SyncPassState passState = new SyncPassState();
        public string technique = "opaque";

        public bool hasLightMap = false;
        public List<string> defines = new List<string>();

        void ModifyValue(Dictionary<string, SyncShaderProperty> propertyMap, string name, string value)
        {
            SyncShaderProperty prop;
            propertyMap.TryGetValue(name, out prop);
            if (prop != null)
            {
                prop.value = value;
            }
        }

        public override void Sync(UnityEngine.Object obj, object param1 = null)
        {
            this.name = "cc.Material";

            Material m = obj as Material;
            var propertyMap = new Dictionary<string, SyncShaderProperty>();

            var meshRenderer = param1 as SyncMeshRendererData;
            if (meshRenderer != null)
            {
                List<UnityEngine.Rendering.ReflectionProbeBlendInfo> probes = new List<UnityEngine.Rendering.ReflectionProbeBlendInfo>();
                meshRenderer.comp.GetClosestReflectionProbes(probes);

                if (probes.Count != 0)
                {
                    defines.Add("USE_IBL=" + 2);
                }

                this.hasLightMap = meshRenderer.lightmapSetting != null;
            }

            var shader = SyncAssetData.GetAssetData<SyncShaderData>(m.shader);
            if (shader != null)
            {
                this.shaderUuid = shader.uuid;
            }


            for (var pi = 0; pi < m.shader.GetPropertyCount(); pi++)
            {
                var type = m.shader.GetPropertyType(pi);
                var name = m.shader.GetPropertyName(pi);

                if (propertyMap.ContainsKey(name))
                {
                    continue;
                }

                var prop = new SyncShaderProperty();
                prop.type = (int)type;
                prop.name = name;

                this.properties.Add(prop);

                propertyMap.Add(name, prop);

                if (type == UnityEngine.Rendering.ShaderPropertyType.Color)
                {
                    prop.value = JsonUtility.ToJson(m.GetColor(name));
                }
                else if (type == UnityEngine.Rendering.ShaderPropertyType.Float || type == UnityEngine.Rendering.ShaderPropertyType.Range)
                {
                    prop.value = m.GetFloat(name).ToString();
                }
                else if (type == UnityEngine.Rendering.ShaderPropertyType.Texture)
                {
                    var t = m.GetTexture(name);
                    if (t)
                    {
                        var tex = SyncAssetData.GetAssetData<SyncTextureData>(t);
                        if (tex != null)
                        {
                            prop.value = tex.uuid;
                        }
                    }
                }
                else if (type == UnityEngine.Rendering.ShaderPropertyType.Vector)
                {
                    prop.value = JsonUtility.ToJson(m.GetVector(name));
                }
            }

            // defines
            SmoothChannel smoothChannel = SmoothChannel.None;

            var shaderKeywords = m.shaderKeywords;
            if (Array.IndexOf(shaderKeywords, "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A") != -1)
            {
                smoothChannel = SmoothChannel.Albedo_A;
            }

            if (m.HasProperty("_MetallicGlossMap") && m.GetTexture("_MetallicGlossMap") != null)
            {
                if (smoothChannel == SmoothChannel.None)
                {
                    smoothChannel = SmoothChannel.Metallic_A;
                }

                ModifyValue(propertyMap, "_Metallic", "1");
            }

            if (smoothChannel != SmoothChannel.None)
            {
                float glossMapScale = 1;
                if (m.HasProperty("_GlossMapScale"))
                {
                    glossMapScale = m.GetFloat("_GlossMapScale");
                }
                ModifyValue(propertyMap, "_Glossiness", glossMapScale.ToString());
                ModifyValue(propertyMap, "_Smoothness", glossMapScale.ToString());
            }

            defines.Add("USE_SMOOTH_CHANNEL=" + (int)smoothChannel);

            // pipeline state
            if (m.HasProperty("_Cull"))
            {
                passState.cullMode = m.GetInt("_Cull");
            }

            // technique
            if (m.HasProperty("_Mode"))
            {
                var mode = m.GetFloat("_Mode");
                if ((BlendMode)mode == BlendMode.Transparent)
                {
                    technique = "transparent";
                }
            }
            else if (m.HasProperty("_Surface"))
            {
                var mode = m.GetFloat("_Surface");
                if (mode == 1)
                {
                    technique = "transparent";
                }
            }
        }

        public override string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }

}