using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace CocosSync
{
    [Serializable]
    class SyncMeshRendererData : SyncComponentData
    {
        public List<string> materilas = new List<string>();
        public string mesh = "";

        public override string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }
}