using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace CocosSync
{
    enum SyncImageDataFormat
    {
        RGBA,
        RGBE,
    }

    [Serializable]
    class SyncImageData
    {
        public float width;
        public float height;
        public List<float> datas = new List<float>();
        public SyncImageDataFormat format = SyncImageDataFormat.RGBA;

        public string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }


    [Serializable]
    class SyncTextureData : SyncAssetData
    {

        float getExponent(float value)
        {
            value = value / 255;
            return Mathf.Clamp(Mathf.Ceil(Mathf.Log(value, 2)), (float)-128.0, (float)127.0);
        }

        void HDR2RGBE(Vector4 hdr, out Vector4 rgbe)
        {
            var maxComp = Mathf.Max(hdr.x, hdr.y, hdr.z);
            var e = Mathf.Clamp(Mathf.Ceil(Mathf.Log(maxComp, 2)) + 128, 0, 255);
            var sc = 1.0 / Math.Pow(2, e - 128 - 8); 

            rgbe.x = Mathf.Clamp((int)(hdr.x * sc), 0, 0xff);
            rgbe.y = Mathf.Clamp((int)(hdr.y * sc), 0, 0xff);
            rgbe.z = Mathf.Clamp((int)(hdr.z * sc), 0, 0xff);
            rgbe.w = e;
        }

        public string image;
        public override void Sync(UnityEngine.Object obj, object param1 = null)
        {
            this.name = "cc.Texture";

            var lowerPath = this.path.ToLower();
            if (!lowerPath.EndsWith(".png") && !lowerPath.EndsWith(".tga"))
            {
                var texture = obj as Texture2D;
                if (texture != null)
                {
                    var image = new SyncImageData();
                    image.width = texture.width;
                    image.height = texture.height;

                    // 创建一张和texture大小相等的临时RenderTexture
                    RenderTexture tmp = RenderTexture.GetTemporary(
                        texture.width,
                        texture.height,
                        0,
                        RenderTextureFormat.DefaultHDR,
                        RenderTextureReadWrite.Linear
                    );

                    // 将texture的像素复制到RenderTexture
                    Graphics.Blit(texture, tmp);
                    // 备份当前设置的RenderTexture
                    RenderTexture previous = RenderTexture.active;
                    // 将创建的临时纹理tmp设置为当前RenderTexture
                    RenderTexture.active = tmp;
                    // 创建一张新的可读Texture2D，并拷贝像素值
                    Texture2D newTexture2D = new Texture2D(texture.width, texture.height);
                    // 将RenderTexture的像素值拷贝到新的纹理中
                    newTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
                    newTexture2D.Apply();
                    // 重置激活的RenderTexture
                    RenderTexture.active = previous;

                    var isHDR = false;
                    if (lowerPath.EndsWith(".exr") || lowerPath.EndsWith(".hdr"))
                    {
                        isHDR = true;
                    }

                    // Gets colors
                    Color[] colors = null;
                    Color32[] colors32 = null;
                    if (isHDR)
                    {
                        colors = newTexture2D.GetPixels(0);
                        image.format = SyncImageDataFormat.RGBE;
                    }
                    else
                    {
                        colors32 = newTexture2D.GetPixels32(0);
                    }

                    var tmpColor = new Vector4();
                    for (var ch = 01; ch < texture.height; ch++)
                    {
                        for (var cw = 0; cw < texture.width; cw++)
                        {
                            var ci = cw + ch * texture.width;
                            if (!isHDR)
                            {
                                tmpColor.x = colors32[ci].r;
                                tmpColor.y = colors32[ci].g;
                                tmpColor.z = colors32[ci].b;
                                tmpColor.w = colors32[ci].a;
                            }
                            else
                            {
                                float scale = (float)1;
                                tmpColor.x = colors[ci].r * scale;
                                tmpColor.y = colors[ci].g * scale;
                                tmpColor.z = colors[ci].b * scale;
                                tmpColor.w = colors[ci].a * scale;

                                HDR2RGBE(tmpColor, out tmpColor);
                            }

                            image.datas.Add(tmpColor.x);
                            image.datas.Add(tmpColor.y);
                            image.datas.Add(tmpColor.z);
                            image.datas.Add(tmpColor.w);
                        }
                    }

                    this.image = image.GetData();
                }
            }
        }

        public override string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }

}