using System;
using System.IO;
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

    enum SyncTextureType
    {
        Texture,
        Cube,
    }

    [Serializable]
    class SyncTextureDataDetail
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
        public int type = (int)SyncTextureType.Texture;

        private Texture texture;
        private bool flipY = false;
        private string extName = "";
        private bool isHDR = false;

        static float getExponent(float value)
        {
            value = value / 255;
            return Mathf.Clamp(Mathf.Ceil(Mathf.Log(value, 2)), (float)-128.0, (float)127.0);
        }

        static void HDR2RGBE(Vector4 hdr, out Vector4 rgbe)
        {
            var maxComp = Mathf.Max(hdr.x, hdr.y, hdr.z);
            var e = Mathf.Clamp(Mathf.Ceil(Mathf.Log(maxComp, 2)) + 128, 0, 255);
            var sc = 1.0 / Math.Pow(2, e - 128 - 8);

            rgbe.x = Mathf.Clamp((int)(hdr.x * sc), 0, 0xff);
            rgbe.y = Mathf.Clamp((int)(hdr.y * sc), 0, 0xff);
            rgbe.z = Mathf.Clamp((int)(hdr.z * sc), 0, 0xff);
            rgbe.w = e;
        }

        Texture2D GetTexture2D()
        {
            if (!texture.isReadable)
            {
                return CreateTexture2D();
            }

            if (extName == ".psd")
            {
                flipY = !flipY;
            }

            if (texture is Texture2D)
            {
                return texture as Texture2D;
            }
            else
            {
                if (texture is Cubemap)
                {
                    var cubemap = texture as Cubemap;
                    Texture2D tex = null;
                    if (cubemap != null)
                    {
                        CubemapFace[] faces = new CubemapFace[] {
                            CubemapFace.PositiveX, CubemapFace.NegativeX,
                            CubemapFace.PositiveY, CubemapFace.NegativeY,
                            CubemapFace.PositiveZ, CubemapFace.NegativeZ
                        };

                        tex = new Texture2D(cubemap.width * 6, cubemap.height, TextureFormat.RGBAFloat, false);
                        var pixels = new List<Color>();
                        for (var h = 0; h < cubemap.height; h++)
                        {
                            foreach (CubemapFace face in faces)
                            {
                                for (var w = 0; w < cubemap.width; w++)
                                {
                                    pixels.Add(cubemap.GetPixel(face, w, h));
                                }
                            }
                        }
                        tex.SetPixels(pixels.ToArray());
                        return tex;
                    }
                }
            }

            return null;
        }

        public Texture2D CreateTexture2D()
        {
            flipY = !flipY;

            var format = RenderTextureFormat.ARGB32;
            var colorSpace = RenderTextureReadWrite.sRGB;
            if (isHDR)
            {
                format = RenderTextureFormat.DefaultHDR;
                colorSpace = RenderTextureReadWrite.Linear;
            }

            // 创建一张和texture大小相等的临时RenderTexture
            RenderTexture tmp = RenderTexture.GetTemporary(
                texture.width,
                texture.height,
                0,
                format,
                colorSpace
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
            newTexture2D.Apply(false, false);
            // 重置激活的RenderTexture
            RenderTexture.active = previous;

            // Graphics.CopyTexture(tmp, newTexture2D);

            return newTexture2D;
        }

        public override void Sync(UnityEngine.Object obj, object param1 = null)
        {
            name = "cc.Texture";

            //
            var lowerPath = this.path.ToLower();
            extName = Path.GetExtension(lowerPath);
            if (extName == ".exr" || extName == ".hdr")
            {
                isHDR = true;
            }
            // 
            texture = obj as Texture;

            if (obj is Texture2D)
            {
                type = (int)SyncTextureType.Texture;
            }
            if (obj is Cubemap)
            {
                type = (int)SyncTextureType.Cube;
            }

            flipY = param1 != null;
        }

        public override string GetData()
        {
            return JsonUtility.ToJson(this);
        }

        public override string GetDetailData()
        {
            var newTexture2D = GetTexture2D();

            var image = new SyncTextureDataDetail();
            image.width = newTexture2D.width;
            image.height = newTexture2D.height;

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

            var start = 0;
            var end = newTexture2D.height;
            var step = 1;
            if (flipY)
            {
                start = newTexture2D.height - 1;
                end = -1;
                step = -1;
            }

            var tmpColor = new Vector4();
            for (var ch = start; ch != end; ch += step)
            {
                for (var cw = 0; cw < newTexture2D.width; cw++)
                {
                    var ci = cw + ch * newTexture2D.width;
                    if (!isHDR)
                    {
                        tmpColor.x = colors32[ci].r;
                        tmpColor.y = colors32[ci].g;
                        tmpColor.z = colors32[ci].b;
                        tmpColor.w = colors32[ci].a;
                    }
                    else
                    {
                        tmpColor.x = colors[ci].r;
                        tmpColor.y = colors[ci].g;
                        tmpColor.z = colors[ci].b;
                        tmpColor.w = colors[ci].a;

                        HDR2RGBE(tmpColor, out tmpColor);
                    }

                    image.datas.Add(tmpColor.x);
                    image.datas.Add(tmpColor.y);
                    image.datas.Add(tmpColor.z);
                    image.datas.Add(tmpColor.w);
                }
            }

            return image.GetData();
        }
    }

}