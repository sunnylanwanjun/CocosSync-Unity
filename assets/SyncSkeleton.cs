
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
        public List<string> bindposes = new List<string>();

        string GetMatrixString(Matrix4x4 mat)
        {
            return
            mat.m00 + "," + mat.m10 + "," + mat.m20 + "," + mat.m30 + "," +
            mat.m01 + "," + mat.m11 + "," + mat.m21 + "," + mat.m31 + "," +
            mat.m02 + "," + mat.m12 + "," + mat.m22 + "," + mat.m32 + "," +
            mat.m03 + "," + mat.m13 + "," + mat.m23 + "," + mat.m33
            ;
        }

        public override void Sync(UnityEngine.Object obj, object param1 = null)
        {
            this.name = "cc.Skeleton";

            var renderer = param1 as SkinnedMeshRenderer;
            var rootBone = Hierarchy.GetRootBone(renderer);

            if (rootBone != null)
            {
                root = rootBone.name;
                for (var i = 0; i < renderer.bones.Length; i++)
                {
                    var bone = renderer.bones[i];
                    var bindpose = renderer.sharedMesh.bindposes[i];

                    bones.Add(Hierarchy.GetPath(bone, rootBone, false));
                    bindposes.Add(GetMatrixString(bindpose));
                }
            }

        }


        public override string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }

}