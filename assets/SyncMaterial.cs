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

    [Serializable]
    class SyncMaterialData : SyncAssetData
    {
        public String shaderUuid = "";
        public List<SyncShaderProperty> properties = new List<SyncShaderProperty>();
        public SyncPassState passState = new SyncPassState();

        public override void Sync(UnityEngine.Object obj)
        {
            this.name = "cc.Material";

            Material m = obj as Material;

            this.shaderUuid = SyncAssetData.GetAssetData<SyncShaderData>(m.shader);

            for (var pi = 0; pi < m.shader.GetPropertyCount(); pi++)
            {
                var type = m.shader.GetPropertyType(pi);
                var name = m.shader.GetPropertyName(pi);

                var prop = new SyncShaderProperty();
                prop.type = (int)type;
                prop.name = name;

                this.properties.Add(prop);

                if (type == UnityEngine.Rendering.ShaderPropertyType.Color)
                {
                    prop.value = JsonUtility.ToJson(m.GetColor(name));
                }
                else if (type == UnityEngine.Rendering.ShaderPropertyType.Float)
                {
                    prop.value = m.GetFloat(name).ToString();
                }
                else if (type == UnityEngine.Rendering.ShaderPropertyType.Texture)
                {
                    var t = m.GetTexture(name);
                    if (t)
                    {
                        prop.value = SyncAssetData.GetAssetData<SyncTextureData>(t);
                    }
                }
                else if (type == UnityEngine.Rendering.ShaderPropertyType.Vector)
                {
                    prop.value = JsonUtility.ToJson(m.GetVector(name));
                }
            }

            if (m.HasProperty("_Cull")) {
                this.passState.cullMode = m.GetInt("_Cull");
            }
        }

        public override string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }

}