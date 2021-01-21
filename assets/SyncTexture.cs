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

    class SyncTextureMipmapDetail
    {
        public float width;
        public float height;
        public List<float> datas = new List<float>();

        public string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [Serializable]
    class SyncTextureDataDetail
    {
        public SyncImageDataFormat format = SyncImageDataFormat.RGBA;
        public List<string> mipmaps = new List<string>();
        public string GetData()
        {
            return JsonUtility.ToJson(this);
        }
    }


    [Serializable]
    class SyncTextureData : SyncAssetData
    {
        public int type = (int)SyncTextureType.Texture;
        public int mipmapCount = 0;

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

        // Texture2D GetTexture2D()
        // {
        //     if (!texture.isReadable)
        //     {
        //         return CreateTexture2D();
        //     }

        //     if (extName == ".psd")
        //     {
        //         flipY = !flipY;
        //     }

        //     if (texture is Texture2D)
        //     {
        //         return texture as Texture2D;
        //     }
        //     else
        //     {
        //         if (texture is Cubemap)
        //         {
        //             var cubemap = texture as Cubemap;
        //             Texture2D tex = null;
        //             if (cubemap != null)
        //             {
        //                 CubemapFace[] faces = new CubemapFace[] {
        //                     CubemapFace.PositiveX, CubemapFace.NegativeX,
        //                     CubemapFace.PositiveY, CubemapFace.NegativeY,
        //                     CubemapFace.PositiveZ, CubemapFace.NegativeZ
        //                 };

        //                 tex = new Texture2D(cubemap.width * 6, cubemap.height, TextureFormat.RGBAFloat, false);
        //                 var pixels = new List<Color>();
        //                 for (var h = 0; h < cubemap.height; h++)
        //                 {
        //                     foreach (CubemapFace face in faces)
        //                     {
        //                         for (var w = 0; w < cubemap.width; w++)
        //                         {
        //                             pixels.Add(cubemap.GetPixel(face, w, h));
        //                         }
        //                     }
        //                 }
        //                 tex.SetPixels(pixels.ToArray());
        //                 return tex;
        //             }
        //         }
        //     }

        //     return null;
        // }

        public Texture2D BlitTexture()
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

        public Texture2D GetTexture2D()
        {
            Texture2D newTexture2D = null;
            if (texture is Cubemap)
            {
                var format = TextureFormat.RGBA32;
                if (isHDR)
                {
                    format = TextureFormat.RGBAFloat;
                }
                newTexture2D = new Texture2D(texture.width * 6, texture.height, format, texture.mipmapCount, true);

                for (var mipLevel = 0; mipLevel < texture.mipmapCount; mipLevel++)
                {
                    var width = (int)(texture.width / Math.Pow(2, mipLevel));
                    var height = (int)(texture.height / Math.Pow(2, mipLevel));

                    var array = new Texture2DArray(texture.width, texture.height, 6, texture.graphicsFormat, UnityEngine.Experimental.Rendering.TextureCreationFlags.MipChain);
                    var allPixels = new List<Color[]>();
                    for (var i = 0; i < 6; i++)
                    {
                        Graphics.CopyTexture(texture, i, array, i);

                        allPixels.Add(array.GetPixels(i, mipLevel));
                    }

                    var pixels = new List<Color>();
                    for (var h = 0; h < height; h++)
                    {
                        for (var fi = 0; fi < 6; fi++)
                        {
                            for (var w = 0; w < width; w++)
                            {
                                var colors = allPixels[fi];
                                pixels.Add(colors[w + h * width]);
                            }
                        }
                    }
                    newTexture2D.SetPixels(pixels.ToArray(), mipLevel);
                }
            }
            else if (texture is Texture2D)
            {
                if (!texture.isReadable)
                {
                    newTexture2D = new Texture2D(texture.width, texture.height, texture.graphicsFormat, UnityEngine.Experimental.Rendering.TextureCreationFlags.MipChain);
                    Graphics.CopyTexture(texture, newTexture2D);
                }
                else
                {
                    if (extName == ".psd")
                    {
                        flipY = !flipY;
                    }
                    newTexture2D = texture as Texture2D;
                }
            }

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
                mipmapCount = 1;
                type = (int)SyncTextureType.Texture;
            }
            if (obj is Cubemap)
            {
                mipmapCount = (obj as Cubemap).mipmapCount;
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
            var detail = new SyncTextureDataDetail();
            if (isHDR)
            {
                detail.format = SyncImageDataFormat.RGBE;
            }

            for (var mipLevel = 0; mipLevel < mipmapCount; mipLevel++)
            {
                var mipmapData = new SyncTextureMipmapDetail();

                var width = (int)(newTexture2D.width / Math.Pow(2, mipLevel));
                var height = (int)(newTexture2D.height / Math.Pow(2, mipLevel));

                mipmapData.width = width;
                mipmapData.height = height;

                // Gets colors
                Color[] colors = null;
                Color32[] colors32 = null;
                if (isHDR)
                {
                    colors = newTexture2D.GetPixels(mipLevel);
                }
                else
                {
                    colors32 = newTexture2D.GetPixels32(mipLevel);
                }

                var start = 0;
                var end = height;
                var step = 1;
                if (flipY)
                {
                    start = height - 1;
                    end = -1;
                    step = -1;
                }

                var tmpColor = new Vector4();
                for (var ch = start; ch != end; ch += step)
                {
                    for (var cw = 0; cw < width; cw++)
                    {
                        var ci = cw + ch * width;
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

                        mipmapData.datas.Add(tmpColor.x);
                        mipmapData.datas.Add(tmpColor.y);
                        mipmapData.datas.Add(tmpColor.z);
                        mipmapData.datas.Add(tmpColor.w);
                    }
                }

                detail.mipmaps.Add(mipmapData.GetData());
            }

            return detail.GetData();
        }
    }

}