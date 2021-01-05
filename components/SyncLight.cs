using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace CocosSync
{
    [Serializable]
    class SyncLightData : SyncComponentData
    {
        public float range = 10;
        public float size = 10;
        public float luminance = 1;
        public List<float> color = new List<float>();

        public override void Sync(Component c)
        {
            var light = c as Light;
            if (light.type == LightType.Point)
            {
                this.name = "cc.SphereLight";
            }
            else if (light.type == LightType.Directional)
            {
                this.name = "cc.DirectionalLight";
            }
            else if (light.type == LightType.Spot)
            {
                this.name = "cc.SpotLight";
            }

            this.range = this.size = light.range;
            this.color.Add(light.color.r);
            this.color.Add(light.color.g);
            this.color.Add(light.color.b);
            this.color.Add(light.color.a);
            this.luminance = light.intensity * (float)3.14 * 3;
        }

        public override string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }
}