using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace CocosSync
{
    [Serializable]
    class SyncAnimatorData : SyncComponentData
    {
        public List<string> clips = new List<string>();
        public List<string> avatarMap = new List<string>();

        public override void Sync(Component c)
        {
            var animator = c as Animator;
            var avatar = animator.avatar;
            if (avatar == null || !avatar.isHuman)
            {
                return;
            }
            this.name = "sync.AnimatorComponent";

            // bone
            for (var i = HumanBodyBones.Hips; i < HumanBodyBones.LastBone; i++)
            {
                var t = animator.GetBoneTransform(i);
                if (t == null)
                {
                    avatarMap.Add("");
                }
                else
                {
                    avatarMap.Add(Hierarchy.GetPath(t, animator.transform, false));
                }
            }

            // animation
            var controller = animator.runtimeAnimatorController;
            var clips = controller.animationClips;
            if (controller is AnimatorOverrideController)
            {
                clips = (controller as AnimatorOverrideController).animationClips;
            }

            foreach (var clip in clips)
            {
                var clipData = SyncAssetData.GetAssetData<SyncAnimationClipData>(clip, avatar.isHuman);
                this.clips.Add(clipData.uuid);
            }
        }

        public override string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }
}