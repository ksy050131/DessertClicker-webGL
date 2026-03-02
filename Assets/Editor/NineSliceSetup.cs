using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to apply 9-slice borders to ScorePanel sprite
/// and set the Image component to Sliced mode.
/// Run via menu: Tools > Apply 9-Slice to ScorePanel
/// </summary>
public class NineSliceSetup : EditorWindow
{
    [MenuItem("Tools/Apply 9-Slice to ScorePanel")]
    public static void Apply()
    {
        string spritePath = "Assets/Arts/UI/ScorePanel.png";

        // Set sprite border for 9-slice
        TextureImporter importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"[NineSliceSetup] Could not find texture at '{spritePath}'.");
            return;
        }

        // Ensure it's a Sprite
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
        }

        // Disable compression for crisp pixel art
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        // Read texture to auto-detect a good border size
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath);
        if (tex != null)
        {
            // Use ~25% of the shorter dimension as border, clamped
            int borderSize = Mathf.Max(8, Mathf.Min(tex.width, tex.height) / 4);
            importer.spriteBorder = new Vector4(borderSize, borderSize, borderSize, borderSize);
            Debug.Log($"[NineSliceSetup] Set sprite border to {borderSize}px on all sides (texture: {tex.width}x{tex.height}).");
        }
        else
        {
            // Fallback
            importer.spriteBorder = new Vector4(16, 16, 16, 16);
            Debug.Log("[NineSliceSetup] Set sprite border to 16px fallback.");
        }

        importer.SaveAndReimport();
        Debug.Log("[NineSliceSetup] ScorePanel sprite 9-slice borders applied. Now set ScorePanel Image type to Sliced in Inspector or via script.");
    }
}
