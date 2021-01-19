using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace CocosSync
{
    public class Hierarchy
    {
        public static string GetPath(Transform t, Transform parent)
        {
            var path = t.name;
            while (t != parent)
            {
                t = t.parent;
                if (t != null)
                {
                    path = t.name + "/" + path;
                }
            }
            return path;
        }
    }
}