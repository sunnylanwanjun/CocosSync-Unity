using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace CocosSync
{
    [Serializable]
    class SyncAnimatorData : SyncComponentData
    {

        public override void Sync(Component c)
        {
            var animator = c as Animator;
            var avatar = animator.avatar;
            if (avatar == null || !avatar.isHuman)
            {
                return;
            }
            this.name = "cc.Animator";

            var humanDescription = avatar.humanDescription;
            var humanBone = humanDescription.human;
            var skeleton = humanDescription.skeleton;

            var controller = animator.runtimeAnimatorController;
            var clips = controller.animationClips;
            if (controller is AnimatorOverrideController)
            {
                clips = (controller as AnimatorOverrideController).animationClips;
            }

            foreach (var clip in clips)
            {

            }
        }

        public override string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }
}