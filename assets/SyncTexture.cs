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

        static Texture2D LoadTexture2D(string filePath, Texture texture)
        {
            // return AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);

            // Texture2D tex = null;
            // byte[] fileData;

            // if (File.Exists(filePath))
            // {
            //     fileData = File.ReadAllBytes(filePath);
            //     tex = new Texture2D(texture.width, texture.height, TextureFormat.RGBAFloat, false);
            //     // tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
            //     tex.LoadRawTextureData(fileData); //..this will auto-resize the texture dimensions.
            // }
            // return tex;

            var cubemap = new Cubemap(texture.width, TextureFormat.RGBAFloat, false);


            // var cubemap = texture as Cubemap;

            var tex = new Texture2D(cubemap.width, cubemap.height, TextureFormat.RGBAFloat, false);
            CubemapFace[] faces = new CubemapFace[] {
                CubemapFace.PositiveX, CubemapFace.NegativeX,
                CubemapFace.PositiveY, CubemapFace.NegativeY,
                CubemapFace.PositiveZ, CubemapFace.NegativeZ };
            foreach (CubemapFace face in faces)
            {
                tex.SetPixels(cubemap.GetPixels(face));
                // File.WriteAllBytes(Application.dataPath + "/" + cubemap.name + "_" + face.ToString() + ".png",
                //                     tex.EncodeToPNG());
            }

            return tex;
        }

        private Texture texture;
        private bool flipY = false;

        public override void Sync(UnityEngine.Object obj, object param1 = null)
        {
            name = "cc.Texture";

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

            var isHDR = false;
            var lowerPath = this.path.ToLower();
            if (lowerPath.EndsWith(".exr") || lowerPath.EndsWith(".hdr"))
            {
                isHDR = true;
            }

            // var format = RenderTextureFormat.ARGB32;
            // var colorSpace = RenderTextureReadWrite.sRGB;
            // if (isHDR)
            // {
            //     format = RenderTextureFormat.DefaultHDR;
            //     colorSpace = RenderTextureReadWrite.Linear;
            // }

            var newTexture2D = texture as Texture2D;
            if (newTexture2D == null)
            {
                newTexture2D = LoadTexture2D("Assets/" + path, texture);
            }

            var image = new SyncTextureDataDetail();
            image.width = texture.width;
            image.height = texture.height;

            // // 创建一张和texture大小相等的临时RenderTexture
            // RenderTexture tmp = RenderTexture.GetTemporary(
            //     texture.width,
            //     texture.height,
            //     0,
            //     format,
            //     colorSpace
            // );

            // // Material material = new Material(Shader.Find("Unlit/Texture"));

            // // 将texture的像素复制到RenderTexture
            // Graphics.Blit(texture, tmp);
            // // 备份当前设置的RenderTexture
            // RenderTexture previous = RenderTexture.active;
            // // 将创建的临时纹理tmp设置为当前RenderTexture
            // RenderTexture.active = tmp;
            // // 创建一张新的可读Texture2D，并拷贝像素值
            // Texture2D newTexture2D = new Texture2D(texture.width, texture.height);
            // // 将RenderTexture的像素值拷贝到新的纹理中
            // newTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            // newTexture2D.Apply();
            // // 重置激活的RenderTexture
            // RenderTexture.active = previous;

            // Graphics.CopyTexture(texture, newTexture2D);


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

            var start = texture.height - 1;
            var end = -1;
            var step = -1;
            if (flipY)
            {
                start = 0;
                end = texture.height;
                step = 1;
            }

            var tmpColor = new Vector4();
            for (var ch = start; ch != end; ch += step)
            {
                for (var cw = 0; cw < texture.width; cw++)
                {
                    var ci = cw + ch * texture.width;
                    if (!isHDR)
                    {
                        var alpha = colors32[ci].a;
                        tmpColor.x = colors32[ci].r;
                        tmpColor.y = colors32[ci].g;
                        tmpColor.z = colors32[ci].b;
                        tmpColor.w = alpha;
                        // if (alpha != 0)
                        // {
                        //     tmpColor.x *= ((float)alpha / 255);
                        //     tmpColor.y *= ((float)alpha / 255);
                        //     tmpColor.z *= ((float)alpha / 255);
                        // }
                    }
                    else
                    {
                        var alpha = colors[ci].a;
                        tmpColor.x = colors[ci].r;
                        tmpColor.y = colors[ci].g;
                        tmpColor.z = colors[ci].b;
                        tmpColor.w = alpha;

                        // if (alpha != 0)
                        // {
                        //     tmpColor.x *= ((float)alpha);
                        //     tmpColor.y *= ((float)alpha);
                        //     tmpColor.z *= ((float)alpha);
                        // }

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