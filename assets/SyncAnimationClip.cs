using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace CocosSync
{
    [Serializable]
    class SyncCurveData
    {
        public string name = "";
        public List<float> keyframes = new List<float>();
    }

    [Serializable]
    class SyncAnimationClipDataDetail
    {
        public List<string> curves = new List<string>();
    }

    [Serializable]
    class SyncAnimationClipData : SyncAssetData
    {
        public bool isHuman = false;

        AnimationClip clip = null;

        public override void Sync(UnityEngine.Object obj, object param1 = null)
        {
            name = "cc.AnimationClip";
            clip = obj as AnimationClip;
            isHuman = param1 != null;
        }
        public override string GetData()
        {
            return JsonUtility.ToJson(this);
        }

        void GetHumanData(SyncAnimationClipDataDetail data)
        {
            var bindings = AnimationUtility.GetCurveBindings(clip);
            foreach (var binding in bindings)
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                curve.Evaluate(0);

                foreach (var key in curve.keys)
                {
                    var frame = key;
                }
            }
        }

        public override string GetDetailData()
        {
            SyncAnimationClipDataDetail data = new SyncAnimationClipDataDetail();

            if (isHuman)
            {
                GetHumanData(data);
            }

            return JsonUtility.ToJson(data);
        }
    }

}