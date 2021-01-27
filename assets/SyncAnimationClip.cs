using System;
using System.IO;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace CocosSync
{
    [Serializable]
    class SyncAnimationCurveData
    {
        public string name = "";
        public List<float> times = new List<float>();
        public List<float> keyframes = new List<float>();

        public string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [Serializable]
    class SyncAnimationNodeData
    {
        public string path = "";
        // public List<SyncAnimationCurveData> curves = new List<SyncAnimationCurveData>();
        public List<string> curves = new List<string>();

        public string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [Serializable]
    class SyncAnimationClipDataDetail
    {
        // public List<SyncAnimationNodeData> nodes = new List<SyncAnimationNodeData>();
        public List<string> nodes = new List<string>();

        public string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [Serializable]
    class SyncAnimationClipData : SyncAssetData
    {
        public bool isHuman = false;
        public float duration = 0;
        public float sample = 0;

        AnimationClip clip = null;

        static string[][] TRSNames = new string[][] {
            new string[] { "T.x", "translation" },
            new string[] { "T.y", "translation" },
            new string[] { "T.z", "translation" },

            new string[] { "Q.x", "rotation" },
            new string[] { "Q.y", "rotation" },
            new string[] { "Q.z", "rotation" },
            new string[] { "Q.w", "rotation" },

            new string[] { "S.x", "scale" },
            new string[] { "S.y", "scale" },
            new string[] { "S.z", "scale" },
        };

        public override void Sync(UnityEngine.Object obj, object param1 = null)
        {
            name = "SyncAnimationClip";
            clip = obj as AnimationClip;
            isHuman = param1 != null;
            duration = clip.length;
            sample = clip.frameRate;
        }
        public override string GetData()
        {
            return JsonUtility.ToJson(this);
        }

        void GetHumanData(SyncAnimationClipDataDetail data)
        {
            var duration = clip.length;
            var frames = clip.frameRate * duration;
            var bindings = AnimationUtility.GetCurveBindings(clip);

            Dictionary<string, List<AnimationCurve>> curves = new Dictionary<string, List<AnimationCurve>>();

            var nodeData = new SyncAnimationNodeData();

            foreach (var binding in bindings)
            {
                foreach (var trsPair in TRSNames)
                {
                    if (binding.propertyName.EndsWith(trsPair[0]))
                    {
                        var boneName = binding.propertyName.Replace(trsPair[0], "");
                        var propertyName = boneName + "." + trsPair[1];

                        List<AnimationCurve> innnerCurves = null;
                        curves.TryGetValue(propertyName, out innnerCurves);

                        if (innnerCurves == null)
                        {
                            innnerCurves = new List<AnimationCurve>();
                            curves.Add(propertyName, innnerCurves);
                        }

                        AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                        innnerCurves.Add(curve);
                    }
                }
            }

            foreach (var innnerCurves in curves)
            {
                var curveData = new SyncAnimationCurveData();
                curveData.name = innnerCurves.Key;

                for (var fi = 0; fi < frames; fi++)
                {
                    var time = fi * 1 / clip.frameRate;
                    curveData.times.Add(time);

                    foreach (var curve in innnerCurves.Value)
                    {
                        var value = curve.Evaluate(time);
                        curveData.keyframes.Add(value);
                    }
                }

                nodeData.curves.Add(curveData.GetData());
            }

            data.nodes.Add(nodeData.GetData());
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