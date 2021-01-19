using System;
using System.IO;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace CocosSync
{
    [Serializable]
    class SyncSkinnedMeshRendererData : SyncMeshRendererData
    {
        public string skeleton;

        public override void Sync(Component c)
        {
            base.Sync(c);
            name = "cc.SkinnedMeshRenderer";

            SkinnedMeshRenderer renderer = c as SkinnedMeshRenderer;

            var meshData = SyncAssetData.GetAssetData<SyncMeshData>(renderer.sharedMesh);
            if (meshData != null)
            {
                var path = Path.ChangeExtension(meshData.path, ".skeleton");

                var asset = new SyncSkeletonData();
                asset.uuid = path;
                asset.path = path;

                asset.shouldCheckSrc = false;
                asset.virtualAsset = true;
                asset.Sync(null, renderer);

                SyncAssetData.AddAssetData(asset);

                skeleton = path;
            }
        }

        public override string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }
}