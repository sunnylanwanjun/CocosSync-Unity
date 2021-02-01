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
        public float cullMode;
        public float blendSrc;
        public float blendDst;
        public float zWrite;
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

    public enum ShaderType
    {
        Standard,
        ShaderGraph,
    }

    [Serializable]
    class SyncMaterialData : SyncAssetData
    {
        public ShaderType shaderType = ShaderType.Standard;
        public String shaderUuid = "";
        public List<SyncShaderProperty> properties = new List<SyncShaderProperty>();
        public List<SyncShaderProperty> extendProperties = new List<SyncShaderProperty>();
        public SyncPassState passState = new SyncPassState();
        public string technique = "opaque";

        public bool hasLightMap = false;
        public List<string> defines = new List<string>();

        static Dictionary<string, string> PropertiesMap = new Dictionary<string, string> {
            // builtin standard
            {"_Color",              "mainColor"},
            {"_MainTex",            "albedoMap"},
            {"_Cutoff",             "alphaThreshold"},
            {"_Metallic",           "metallic"},
            {"_MetallicGlossMap",   "metallicGlossMap"},
            {"_BumpScale",          "normalStrenth"},
            {"_BumpMap",            "normalMap"},
            {"_OcclusionStrength",  "occlusion"},
            {"_EmissionColor",      "emissive"},
            {"_EmissionMap",        "emissiveMap"},

            // urp lit
            {"_BaseColor",          "mainColor"},
            {"_BaseMap",            "albedoMap"},
            {"_Smoothness",         "smoothness"},

            // hdr lit
            {"_BaseColorMap",       "albedoMap"},
            {"_NormalMap",          "normalMap"},
            {"_NormalScale",         "normalStrenth"},

        };

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

            var rendererData = param1 as SyncRendererData;
            if (rendererData != null && rendererData.comp != null)
            {
                List<UnityEngine.Rendering.ReflectionProbeBlendInfo> probes = new List<UnityEngine.Rendering.ReflectionProbeBlendInfo>();
                rendererData.comp.GetClosestReflectionProbes(probes);

                if (probes.Count != 0)
                {
                    defines.Add("USE_IBL=" + 2);
                }

                this.hasLightMap = rendererData.lightmapSetting != null;
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

                if (PropertiesMap.ContainsKey(name))
                {
                    PropertiesMap.TryGetValue(name, out prop.name);
                    this.properties.Add(prop);
                }
                else
                {
                    prop.name = name;
                    this.extendProperties.Add(prop);
                }

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

            if (m.enableInstancing)
            {
                defines.Add("USE_INSTANCING=" + m.enableInstancing);
            }

            // pipeline state
            if (m.HasProperty("_Cull"))
            {
                passState.cullMode = m.GetFloat("_Cull");
            }
            if (m.HasProperty("_DstBlend"))
            {
                passState.blendSrc = m.GetInt("_DstBlend");
            }
            if (m.HasProperty("_DstBlend"))
            {
                passState.blendDst = m.GetInt("_DstBlend");
            }
            if (m.HasProperty("_ZWrite"))
            {
                passState.zWrite = m.GetInt("_ZWrite");
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