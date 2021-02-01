using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace CocosSync
{
    public class Hierarchy
    {
        public static string GetPath(Transform t, Transform parent, bool includeParent = true)
        {
            var path = t.name;
            while (t != parent)
            {
                t = t.parent;

                if (t == parent && !includeParent)
                {
                    break;
                }

                if (t != null)
                {
                    path = t.name + "/" + path;
                }
            }
            return path;
        }

        public static Transform GetRootBone(SkinnedMeshRenderer renderer)
        {
            var rootBone = renderer.rootBone;

            var parent = rootBone.parent;
            while (parent)
            {
                if (parent.GetComponent<Animator>() != null)
                {
                    rootBone = parent;
                    break;
                }
                parent = parent.parent;
            }

            return rootBone;
        }
    }
}