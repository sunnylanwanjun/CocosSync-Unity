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

        public bool shouldCheckSrc = true;

        public static IDataType GetAssetData<IDataType>(UnityEngine.Object obj, object param1 = null) where IDataType : SyncAssetData, new()
        {
            if (obj == null)
            {
                return null;
            }

            var path = AssetDatabase.GetAssetPath(obj);

            string uuid;
            long file;
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out uuid, out file))
            {
                Debug.LogWarning("Can not find guid for asset.");
                return null;
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
                return asset as IDataType;
            }

            asset = new IDataType();

            asset.uuid = uuid + "/" + obj.name;

            asset.path = path;
            asset.path = asset.path.Replace("Assets/", "");

            asset.Sync(obj, param1);

            assetPack.Add(obj.name, asset);

            if (asset is SyncTextureData)
            {
                CocosSyncTool.sceneData.assets.Insert(0, asset.GetData());
            }
            else
            {
                CocosSyncTool.sceneData.assets.Add(asset.GetData());
            }

            return asset as IDataType;
        }

        public virtual void Sync(UnityEngine.Object obj, object param1 = null)
        {
            this.name = "cc.Asset";
        }

        public virtual string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }
}