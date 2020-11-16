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

        public override void Sync(Component c)
        {
            Terrain terrainObject = c as Terrain;

            this.name = "cc.Terrain";

            TerrainData terrain = terrainObject.terrainData;

            var terrainLayers = terrain.terrainLayers;
            var alphaMaps = terrain.GetAlphamaps(0, 0, terrain.alphamapWidth, terrain.alphamapHeight);

            for (var i = 0; i < terrainLayers.Length; i++)
            {
                SyncTerrainLayer layer = new SyncTerrainLayer();
                layer.name = terrainLayers[i].name;
                this.terrainLayers.Add(layer);
            }

            // weight datas
            int weightmapWidth = terrain.alphamapWidth;
            int weightmapHeight = terrain.alphamapHeight;

            float[] allWeightDatas = new float[weightmapWidth * weightmapHeight * terrainLayers.Length];
            for (var i = 0; i < weightmapWidth; i++)
            {
                for (var j = 0; j < weightmapHeight; j++)
                {
                    for (var k = 0; k < terrainLayers.Length; k++)
                    {
                        var value = alphaMaps[j, i, k];
                        if (Single.IsNaN(value))
                        {
                            value = 0;
                        }

                        int index = (i + j * weightmapWidth) * terrainLayers.Length + k;
                        allWeightDatas[index] = value;
                    }
                }
            }


            // height datas
            int heightmapWidth = terrain.heightmapResolution;
            int heightmapHeight = terrain.heightmapResolution;

            var tData = terrain.GetHeights(0, 0, heightmapWidth, heightmapHeight);
            var height = terrain.size.y;

            float[] allHeightDatas = new float[heightmapWidth * heightmapHeight];
            for (var i = 0; i < heightmapWidth; i++)
            {
                for (var j = 0; j < heightmapHeight; j++)
                {
                    allHeightDatas[i + j * heightmapWidth] = tData[j, i] * height;
                }
            }

            this.weightDatas = allWeightDatas;
            this.heightmapWidth = heightmapWidth;
            this.heightmapHeight = heightmapHeight;

            this.heightDatas = allHeightDatas;
            this.weightmapWidth = weightmapWidth;
            this.weightmapHeight = weightmapHeight;

            this.terrainWidth = terrain.size.x;
            this.terrainHeight = terrain.size.y;
        }


        public override string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }
}