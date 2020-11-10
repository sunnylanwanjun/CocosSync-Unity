using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace CocosSync
{

    [Serializable]
    class SyncMaterialData : SyncAssetData
    {
        public override void Sync(UnityEngine.Object obj)
        {
            this.name = "cc.Material";

            Material m = obj as Material;
        }

        public override string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }

}