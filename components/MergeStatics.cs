using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;


namespace CocosSync
{
    [Serializable]
    class SyncMergeStaticsData : SyncComponentData
    {
        public float mergeSize = 10;

        public override void Sync(Component c)
        {
            MergeStatics comp = c as MergeStatics;

            this.name = "MergeStatics";
            this.mergeSize = comp.mergeSize;
        }

        public override string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }

    public class MergeStatics : MonoBehaviour
    {
        public float mergeSize = 10;

        private void OnEnable()
        {

        }
        private void OnDisable()
        {

        }
    }
}