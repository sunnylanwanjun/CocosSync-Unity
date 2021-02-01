using System;
using System.IO;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace CocosSync
{
    [Serializable]
    class SyncSkinnedMeshRendererData : SyncRendererData
    {
        public string skeleton;
        public string mesh;
        public string rootBonePath;

        public override void Sync(Component c)
        {
            base.Sync(c);
            name = "cc.SkinnedMeshRenderer";

            SkinnedMeshRenderer renderer = c as SkinnedMeshRenderer;

            var meshData = SyncAssetData.GetAssetData<SyncMeshData>(renderer.sharedMesh);
            if (meshData != null)
            {
                mesh = meshData.uuid;
            }

            if (meshData != null)
            {
                var path = Path.Combine(Path.GetDirectoryName(meshData.path), Path.GetFileNameWithoutExtension(meshData.path), renderer.transform.name + ".skeleton");

                var asset = new SyncSkeletonData();
                asset.uuid = path;
                asset.path = path;

                asset.shouldCheckSrc = false;
                asset.virtualAsset = true;
                asset.Sync(null, renderer);

                SyncAssetData.AddAssetData(asset);

                skeleton = path;
            }

            var rootBone = Hierarchy.GetRootBone(renderer);

            // CocosSyncTool.Instance.SyncNode(rootBone);
            rootBonePath = Hierarchy.GetPath(rootBone, null);
        }

        public override string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }
}