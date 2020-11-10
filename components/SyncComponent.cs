using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace CocosSync
{
    [Serializable]
    class SyncComponentData
    {
        public String uuid;
        public String name;

        public void Sync(Component c)
        {
            this.name = c.name;
        }

        public virtual string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }
}