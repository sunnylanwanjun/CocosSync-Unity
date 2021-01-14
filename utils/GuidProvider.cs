using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace CocosSync
{
    [ExecuteInEditMode]
    public class GuidProvider : MonoBehaviour
    {
#if UNITY_EDITOR
        static Dictionary<string, bool> guidMap = new Dictionary<string, bool>();
#endif

        public string guid = System.Guid.NewGuid().ToString();

        void ReGenerate()
        {
            guid = System.Guid.NewGuid().ToString();
        }

        public void Reset()
        {
            if (GuidProvider.guidMap.ContainsKey(guid))
            {
                GuidProvider.guidMap.Remove(guid);
            }
            guid = System.Guid.NewGuid().ToString();
            GuidProvider.guidMap.Add(guid, true);
        }

        void Awake()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (GuidProvider.guidMap.ContainsKey(guid))
                {
                    ReGenerate();
                }
                GuidProvider.guidMap.Add(guid, true);
            }
#endif
        }
    }
}