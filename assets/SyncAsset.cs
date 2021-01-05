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

            uuid = uuid + "/" + obj.name;

            SyncAssetData asset = null;
            CocosSyncTool.sceneData.assetsMap.TryGetValue(uuid, out asset);

            if (asset != null)
            {
                return asset as IDataType;
            }

            asset = new IDataType();

            asset.uuid = uuid;
            
            asset.path = path;
            asset.path = asset.path.Replace("Assets/", "");

            asset.Sync(obj, param1);

            AddAssetData(asset, false);

            return asset as IDataType;
        }

        public static void AddAssetData(SyncAssetData asset, bool checkExists = true)
        {
            if (checkExists)
            {
                SyncAssetData tmpAsset = null;
                CocosSyncTool.sceneData.assetsMap.TryGetValue(asset.uuid, out tmpAsset);

                if (tmpAsset != null)
                {
                    CocosSyncTool.sceneData.assetsMap.Remove(asset.uuid);
                    CocosSyncTool.sceneData.assetsMap.Add(asset.uuid, asset);
                    return;
                }
            }

            CocosSyncTool.sceneData.assetsMap.Add(asset.uuid, asset);
        }

        public virtual void Sync(UnityEngine.Object obj, object param1 = null)
        {
            this.name = "cc.Asset";
        }

        public virtual string GetData()
        {
            return JsonUtility.ToJson(this);
        }

        public virtual string GetDetailData()
        {
            return "";
        }
    }
}