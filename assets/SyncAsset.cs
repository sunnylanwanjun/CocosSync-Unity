using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace CocosSync
{

    [Serializable]
    class SyncAssetData
    {
        public String name;
        public String uuid;
        public String path;

        public static string GetAssetData<IDataType>(UnityEngine.Object obj) where IDataType : SyncAssetData, new()
        {
            if (obj == null)
            {
                return "";
            }

            string uuid;
            long file;
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out uuid, out file))
            {
                Debug.LogWarning("Can not find guid for asset.");
                return "";
            }

            Dictionary<string, SyncAssetData> assetPack;
            CocosSyncTool.sceneData.assetsMap.TryGetValue(uuid, out assetPack);

            if (assetPack == null)
            {
                assetPack = new Dictionary<string, SyncAssetData>();
                CocosSyncTool.sceneData.assetsMap.Add(uuid, assetPack);
            }


            SyncAssetData asset = null;
            assetPack.TryGetValue(obj.name, out asset);

            if (asset != null)
            {
                return asset.uuid;
            }

            asset = new IDataType();

            asset.uuid = uuid + "/" + obj.name;

            asset.path = AssetDatabase.GetAssetPath(obj);
            asset.path = asset.path.Replace("Assets/", "");

            asset.Sync(obj);

            assetPack.Add(obj.name, asset);
            CocosSyncTool.sceneData.assets.Add(asset.GetData());

            return asset.uuid;
        }

        public virtual void Sync(UnityEngine.Object obj)
        {
            this.name = "cc.Asset";
        }

        public virtual string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }
}