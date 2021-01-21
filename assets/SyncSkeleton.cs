
using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace CocosSync
{


    [Serializable]
    class SyncSkeletonData : SyncAssetData
    {
        public string root = "";
        public List<string> bones = new List<string>();
        public List<string> matrices = new List<string>();

        string GetPath(Transform root, Transform child)
        {
            var path = child.name;
            while (child != root)
            {
                path = root.name + "/" + path;
                child = child.parent;
            }
            return path;
        }
        string GetMatrix(Transform node)
        {
            return Matrix4x4.TRS(node.position, node.rotation, node.localScale).ToString();
        }

        public override void Sync(UnityEngine.Object obj, object param1 = null)
        {
            this.name = "cc.Skeleton";

            var renderer = param1 as SkinnedMeshRenderer;
            var rootBone = renderer.rootBone;

            root = rootBone.name;
            foreach (var bone in renderer.bones)
            {
                bones.Add(GetPath(rootBone, bone));
                matrices.Add(GetMatrix(bone));
            }
        }


        public override string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }

}