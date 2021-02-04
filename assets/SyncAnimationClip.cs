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

        public Vector3 GetLastVector3 (int index) {
            index = values.Count - (index + 1) * 3;
            Vector3 vec = new Vector3();
            if (index >= 0) {
                vec.x = values[index];
                vec.y = values[index + 1];
                vec.z = values[index + 2];
            }
            return vec;
        }

        public Quaternion GetLastQuat (int index) {
            index = values.Count - (index + 1) * 4;
            Quaternion quat = new Quaternion();
            if (index >= 0) {
                quat.x = values[index];
                quat.y = values[index + 1];
                quat.z = values[index + 2];
                quat.w = values[index + 3];
            }
            return quat;
        }

        public Boolean IsLastVector3Same () {
            var enough = this.IsEnoughVector3(3);
            if (!enough) return false;
            var last0 = GetLastVector3(0);
            var last1 = GetLastVector3(1);
            var last2 = GetLastVector3(2);
            if (!last0.Equals(last1)) return false;
            if (!last0.Equals(last2)) return false;
            return true;
        }

        public Boolean IsLastQuatSame () {
            var enough = this.IsEnoughQuat(3);
            if (!enough) return false;
            var last0 = GetLastQuat(0);
            var last1 = GetLastQuat(1);
            var last2 = GetLastQuat(2);
            if (!last0.Equals(last1)) return false;
            if (!last0.Equals(last2)) return false;
            return true;
        }

        public Boolean IsEnoughVector3 (int count) {
            return values.Count / 3 > count;
        }

        public Boolean IsEnoughQuat (int count) {
            return values.Count / 4 > count;
        }

        public void RemoveLastVector3Value (int index) {
            index = values.Count - (index + 1) * 3;
            values.RemoveRange(index, 3);
        }

        public void RemoveLastQuatValue (int index) {
            index = values.Count - (index + 1) * 4;
            values.RemoveRange(index, 4);
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

    class AnimationClipParam {
        public Animator animator;
        public string stateName;
        public string folderName;
    };

    [Serializable]
    class SyncAnimationClipData : SyncAssetData
    {
        public bool isHuman = false;
        public float duration = 0;
        public float sample = 0;
        public string animName = "";
        public string stateName = "";
        public string folderName = "";

        Animator animator = null;
        AnimationClip clip = null;

        int curvesIndex = 0;
        int keysIndex = 0;

        public override void Sync(UnityEngine.Object obj, object param1 = null)
        {
            name = "SyncAnimationClip";
            clip = obj as AnimationClip;
            duration = clip.length;
            sample = clip.frameRate;
            animName = obj.name;

            var param = param1 as AnimationClipParam;
            animator = param.animator;
            stateName = param.stateName;
            folderName = param.folderName;
        }

        public override string GetData()
        {
            return JsonUtility.ToJson(this);
        }

        void traverseTransform (Transform transform, List<SyncAnimationCurveData> curves, List<List<float>> keysVal, float currentTime, string parentPath, bool ignoreCurrentNode, bool isRoot) {
            if (!transform.gameObject.activeSelf) return;

            if (ignoreCurrentNode) {
                for (var i = 0; i < transform.childCount; i++) {
                    traverseTransform(transform.GetChild(i), curves, keysVal, currentTime, parentPath, false, false);
                }
                return;
            }

            SyncAnimationCurveData translateCurveData;
            SyncAnimationCurveData scaleCurveData;
            SyncAnimationCurveData rotationCurveData;
            
            List<float> translateTimeVal;
            List<float> scaleTimeVal;
            List<float> rotationTimeVal;

            if (keysVal.Count > keysIndex) {
                translateTimeVal = keysVal[keysIndex];
                scaleTimeVal = keysVal[keysIndex + 1];
                rotationTimeVal = keysVal[keysIndex + 2];
            } else {
                translateTimeVal = new List<float>();
                keysVal.Add(translateTimeVal);

                scaleTimeVal = new List<float>();
                keysVal.Add(scaleTimeVal);

                rotationTimeVal = new List<float>();
                keysVal.Add(rotationTimeVal);
            }

            if (curves.Count > curvesIndex) {
                translateCurveData = curves[curvesIndex];
                scaleCurveData = curves[curvesIndex + 1];
                rotationCurveData = curves[curvesIndex + 2];
            } else {
                translateCurveData = new SyncAnimationCurveData();
                translateCurveData.name = TransformType.Translation;
                translateCurveData.key = keysIndex;
                curves.Add(translateCurveData);

                scaleCurveData = new SyncAnimationCurveData();
                scaleCurveData.name = TransformType.Scale;
                scaleCurveData.key = keysIndex + 1;
                curves.Add(scaleCurveData);

                rotationCurveData = new SyncAnimationCurveData();
                rotationCurveData.name = TransformType.Rotation;
                rotationCurveData.key = keysIndex + 2;
                curves.Add(rotationCurveData);
            }

            if (!isRoot) {
                if (parentPath != "") {
                    parentPath += "/" + transform.name;
                } else {
                    parentPath += transform.name;
                }
            }

            var posSame = translateCurveData.IsLastVector3Same();
            var scaleSame = scaleCurveData.IsLastVector3Same();
            var rotationSame = rotationCurveData.IsLastQuatSame();

            if (posSame) {
                translateCurveData.RemoveLastVector3Value(1);
                translateTimeVal.RemoveAt(translateTimeVal.Count - 2);
            }

            if (scaleSame) {
                scaleCurveData.RemoveLastVector3Value(1);
                scaleTimeVal.RemoveAt(scaleTimeVal.Count - 2);
            }

            if (rotationSame) {
                rotationCurveData.RemoveLastQuatValue(1);
                rotationTimeVal.RemoveAt(rotationTimeVal.Count - 2);
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

            translateTimeVal.Add(currentTime);
            scaleTimeVal.Add(currentTime);
            rotationTimeVal.Add(currentTime);

            curvesIndex += 3;
            keysIndex += 3;
            
            for (var i = 0; i < transform.childCount; i++) {
                traverseTransform(transform.GetChild(i), curves, keysVal, currentTime, parentPath, false, false);
            }
        }

        void GetDetailData(SyncAnimationClipDataDetail data)
        {
            List<SyncAnimationCurveData> curves = new List<SyncAnimationCurveData>();
            List<List<float>> keysVal = new List<List<float>>();

            float dt = 1.0f / 40.0f;
            animator.Play(stateName, -1, 0);
            var animationState = animator.GetCurrentAnimatorStateInfo(0);

            float currentDt = 0.0f;
            float currentTime = 0.0f;
            bool rootMotion = animator.applyRootMotion;

            Debug.Log("Bake Animation:" + animName + " TotalTime:" + duration + " StateName:" + stateName);
            while (true) {
                currentDt = duration - currentTime;
                if (dt < currentDt) {
                    currentDt = dt;
                }
                animator.Update(currentDt);
                curvesIndex = 0;
                keysIndex = 0;
                traverseTransform(animator.transform, curves, keysVal, currentTime, "", !rootMotion, true);
                currentTime += dt;
                if (currentTime >= duration) {
                    break;
                }
            }

            for (int i = 0; i < curves.Count; i ++) {
                data.curves.Add(curves[i].GetData());
            }

            // List<List<float>> to json by JsonUtility has some bug, so transfer by manual
            data.keys = "[";
            for (int i = 0; i < keysVal.Count; i++) {
                var itemVal = keysVal[i];
                var itemKeys = "[";
                for (int j = 0; j < itemVal.Count; j++) {
                    if (j == itemVal.Count - 1) {
                        itemKeys += itemVal[j];
                    } else {
                        itemKeys += itemVal[j] + ",";
                    }
                }
                if (i == keysVal.Count - 1) {
                    itemKeys += "]";
                } else {
                    itemKeys += "],";
                }
                data.keys += itemKeys;
            }
            data.keys += "]";
        }

        public override string GetDetailData()
        {
            SyncAnimationClipDataDetail data = new SyncAnimationClipDataDetail();
            GetDetailData(data);
            return JsonUtility.ToJson(data);
        }
    }

}