using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace CocosSync
{
    [Serializable]
    class SyncAnimatorData : SyncComponentData
    {
        public List<string> clips = new List<string>();

        public override void Sync(Component c)
        {
            this.name = "cc.SkeletalAnimation";

            var motionsComp = c as Motions;
            var animator = c.GetComponent<Animator>();
            if (!animator) {
                animator = c.gameObject.AddComponent<Animator>();                
            }
            
            var animatorCtrl = new AnimatorController();
            animator.runtimeAnimatorController = animatorCtrl;
            
            animatorCtrl.AddLayer("Temp");

            int stateIndex = 0;
            foreach (var motion in motionsComp.motions) {
                if (!motion) continue;

                var state = animatorCtrl.AddMotion(motion, 0);
                var clips = animatorCtrl.animationClips;
                
                var param = new AnimationClipParam();
                param.animator = animator;
                param.stateName = "state:" + stateIndex + "(" + state.name + ")";
                param.folderName = motionsComp.folderName;

                state.name = param.stateName;
                stateIndex++;

                var clipData = SyncAssetData.GetAssetData<SyncAnimationClipData>(clips[clips.Length - 1], param);
                this.clips.Add(clipData.uuid);
            }
        }

        public override string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }
}