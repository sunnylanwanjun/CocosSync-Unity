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
        public List<float> values = new List<float>();
        public string path = "";
        public int key = 0;

        public string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [Serializable]
    class SyncAnimationClipDataDetail
    {
        public List<string> curves = new List<string>();
        public string keys = "";

        public string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }

    class TransformType {
        public static string Translation = "position";
        public static string Rotation = "rotation";
        public static string Scale = "scale";
    };

    [Serializable]
    class SyncAnimationClipData : SyncAssetData
    {
        public bool isHuman = false;
        public float duration = 0;
        public float sample = 0;
        public string animName = "";

        Animator animator = null;
        AnimationClip clip = null;

        int key = 0;

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

        public override void Sync(UnityEngine.Object obj, object param1 = null, object param2 = null)
        {
            name = "SyncAnimationClip";
            clip = obj as AnimationClip;
            animator = param2 as Animator;
            isHuman = param1 != null;
            duration = clip.length;
            sample = clip.frameRate;
            animName = obj.name;
        }

        public override string GetData()
        {
            return JsonUtility.ToJson(this);
        }

        void traverseTransform (Transform transform, List<SyncAnimationCurveData> curves, string parentPath, bool ignore) {

            if (ignore) {
                for (var i = 0; i < transform.childCount; i++) {
                    traverseTransform(transform.GetChild(i), curves, parentPath, false);
                }
                return;
            }

            SyncAnimationCurveData translateCurveData;
            SyncAnimationCurveData scaleCurveData;
            SyncAnimationCurveData rotationCurveData;

            if (curves.Count > key) {
                translateCurveData = curves[key];
                scaleCurveData = curves[key + 1];
                rotationCurveData = curves[key + 2];
            } else {
                translateCurveData = new SyncAnimationCurveData();
                translateCurveData.name = TransformType.Translation;
                translateCurveData.key = key / 3;
                curves.Add(translateCurveData);

                scaleCurveData = new SyncAnimationCurveData();
                scaleCurveData.name = TransformType.Scale;
                scaleCurveData.key = key / 3;
                curves.Add(scaleCurveData);

                rotationCurveData = new SyncAnimationCurveData();
                rotationCurveData.name = TransformType.Rotation;
                rotationCurveData.key = key / 3;
                curves.Add(rotationCurveData);
            }

            key += 3;

            if (parentPath != "") {
                parentPath += "/" + transform.name;
            } else {
                parentPath += transform.name;
            }

            translateCurveData.values.Add(transform.localPosition.x);
            translateCurveData.values.Add(transform.localPosition.y);
            translateCurveData.values.Add(transform.localPosition.z);
            translateCurveData.path = parentPath;

            scaleCurveData.values.Add(transform.localScale.x);
            scaleCurveData.values.Add(transform.localScale.y);
            scaleCurveData.values.Add(transform.localScale.z);
            scaleCurveData.path = parentPath;

            var quat = Quaternion.Euler(transform.localEulerAngles.x, transform.localEulerAngles.y, transform.localEulerAngles.z);
            
            rotationCurveData.values.Add(quat.x);
            rotationCurveData.values.Add(quat.y);
            rotationCurveData.values.Add(quat.z);
            rotationCurveData.values.Add(quat.w);
            rotationCurveData.path = parentPath;

            for (var i = 0; i < transform.childCount; i++) {
                traverseTransform(transform.GetChild(i), curves, parentPath, false);
            }
        }

        void GetHumanData(SyncAnimationClipDataDetail data)
        {
            List<SyncAnimationCurveData> curves = new List<SyncAnimationCurveData>();
            string keys = "[0";

            float dt = 1.0f / 40.0f;
            animator.Play("Empty", -1, 0);
            animator.Update(dt);

            animator.Play(animName, -1, 0);
            var animationState = animator.GetCurrentAnimatorStateInfo(0);
            
            float currentDt = 0.0f;
            float currentTime = 0.0f;

            Debug.Log("Bake Animation:" + animName + " TotalTime:" + duration);
            while (true) {
                currentDt = duration - currentTime;
                if (dt < currentDt) {
                    currentDt = dt;
                }
                animator.Update(currentDt);
                key = 0;
                traverseTransform(animator.transform, curves, "", true);
                currentTime += dt;
                if (currentTime >= duration) {
                    keys += "]";
                    break;
                }
                keys += "," +  currentTime;
            }

            data.keys = "[" + keys;
            for (int i = 0; i < curves.Count; i += 3) {
                data.curves.Add(curves[i].GetData());
                data.curves.Add(curves[i + 1].GetData());
                data.curves.Add(curves[i + 2].GetData());
                data.keys += "," + keys;
            }
            data.keys += "]";

            /*
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
            */
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