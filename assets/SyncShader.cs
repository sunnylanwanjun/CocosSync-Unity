using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace CocosSync
{

    [Serializable]
    class SyncShaderData : SyncAssetData
    {
        public override void Sync(UnityEngine.Object obj, object param1 = null)
        {
            this.name = "cc.Shader";
        }

        public override string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }

}