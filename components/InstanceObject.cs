using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;


namespace CocosSync
{
    [Serializable]
    class SyncInstanceObjectData : SyncComponentData
    {
        public float mergeSize = 10;

        public override void Sync(Component c)
        {
            InstanceObject comp = c as InstanceObject;

            this.name = "InstanceObject";
            this.mergeSize = comp.mergeSize;
        }

        public override string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }

    public class InstanceObject : MonoBehaviour
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