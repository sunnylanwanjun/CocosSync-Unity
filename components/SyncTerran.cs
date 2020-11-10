using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace CocosSync
{
    [Serializable]
    class SyncTerrainLayer
    {
        public string name;
    }

    [Serializable]
    class SyncTerrainData : SyncComponentData
    {
        // height map
        public float heightmapWidth;
        public float heightmapHeight;
        public float[] heightDatas;

        // weight map
        public List<SyncTerrainLayer> terrainLayers = new List<SyncTerrainLayer>();
        public float weightmapWidth;
        public float weightmapHeight;

        public float[] weightDatas;

        // info
        public float terrainWidth;
        public float terrainHeight;


        public override string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }
}