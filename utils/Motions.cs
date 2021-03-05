using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace CocosSync
{
    public class Motions : MonoBehaviour
    {
        public string folderName = "";
        public List<Motion> motions = new List<Motion>{null};
    }
}