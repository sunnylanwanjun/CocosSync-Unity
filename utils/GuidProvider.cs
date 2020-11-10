using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CocosSync
{
    public class GuidProvider : MonoBehaviour
    {
        public string guid = System.Guid.NewGuid().ToString();
    }
}