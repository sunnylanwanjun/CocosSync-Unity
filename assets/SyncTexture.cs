using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace CocosSync
{

    [Serializable]
    class SyncTextureData : SyncAssetData
    {
        public override void Sync(UnityEngine.Object obj)
        {
            this.name = "cc.Texture";
        }

        public override string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }

}