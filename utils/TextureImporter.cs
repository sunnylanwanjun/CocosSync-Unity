
using UnityEditor;
using UnityEngine;

public class PostprocessImages : AssetPostprocessor
{
    void OnPostprocessTexture(Texture2D texture)
    {
        TextureImporter textureImporter = (TextureImporter)assetImporter;
        textureImporter.isReadable = true;
    }
    void OnPostprocessCubemap(Cubemap texture)
    {
        TextureImporter textureImporter = (TextureImporter)assetImporter;
        textureImporter.isReadable = true;
    }
}
