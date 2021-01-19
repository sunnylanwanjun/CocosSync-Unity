using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;


namespace CocosSync
{
    [Serializable]
    class SyncReflectionProbeData : SyncComponentData
    {
        public string bakedTexture = "";

        public override void Sync(Component c)
        {
            this.name = "sync.ReflectionProbe";

            ReflectionProbe comp = c as ReflectionProbe;
            var textureData = SyncAssetData.GetAssetData<SyncTextureData>(comp.bakedTexture);
            if (textureData != null)
            {
                bakedTexture = textureData.uuid;
            }
        }

        public override string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }
}