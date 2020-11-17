using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace CocosSync
{

    [Serializable]
    class SyncMaterialData : SyncAssetData
    {
        public String shaderUuid = "";

        public override void Sync(UnityEngine.Object obj)
        {
            this.name = "cc.Material";

            Material m = obj as Material;

            this.shaderUuid = SyncAssetData.GetAssetData<SyncShaderData>(m.shader);
        }

        public override string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }

}