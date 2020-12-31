using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace CocosSync
{
    [Serializable]
    class SyncImageData
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
    class SyncTextureData : SyncAssetData
    {
        public string image;
        public override void Sync(UnityEngine.Object obj)
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
                        RenderTextureFormat.ARGB32,
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

                    // Gets colors
                    var colors = newTexture2D.GetPixels(0);
                    // var colors = texture.GetPixels32(0);
                    for (var ch = 01; ch < texture.height; ch++)
                    {
                        for (var cw = 0; cw < texture.width; cw++)
                        {
                            var ci = cw + ch * texture.width;
                            image.datas.Add(colors[ci].r);
                            image.datas.Add(colors[ci].g);
                            image.datas.Add(colors[ci].b);
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