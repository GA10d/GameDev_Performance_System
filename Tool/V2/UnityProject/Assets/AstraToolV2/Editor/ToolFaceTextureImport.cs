using UnityEditor;
using UnityEngine;

public sealed class ToolFaceTextureImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(!assetPath.Contains("/AstraToolV2/Resources/FaceMarks/"))return;
        var texture=(TextureImporter)assetImporter;
        texture.textureType=TextureImporterType.Default;
        texture.sRGBTexture=false;
        texture.alphaSource=TextureImporterAlphaSource.None;
        texture.wrapMode=TextureWrapMode.Clamp;
        texture.filterMode=FilterMode.Bilinear;
        texture.mipmapEnabled=true;
        texture.textureCompression=TextureImporterCompression.Uncompressed;
        texture.maxTextureSize=512;
    }
}
