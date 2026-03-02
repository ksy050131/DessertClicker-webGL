using UnityEngine;
using UnityEditor;
using TMPro;
using System.IO;

/// <summary>
/// Editor utility to generate a TMP SDF Font Asset from Galmuri11.ttf
/// with Korean character support. Run via menu: Tools > Generate Korean TMP Font.
/// </summary>
public class KoreanFontGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Korean TMP Font")]
    public static void Generate()
    {
        string ttfPath = "Assets/fonts/Galmuri11.ttf";
        string outputFolder = "Assets/fonts";
        string outputName = "Galmuri11 SDF";
        string outputPath = $"{outputFolder}/{outputName}.asset";

        // Load the TTF font
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
        if (sourceFont == null)
        {
            Debug.LogError($"[KoreanFontGenerator] Font not found at '{ttfPath}'.");
            return;
        }

        // Check if already generated
        if (File.Exists(outputPath))
        {
            Debug.Log($"[KoreanFontGenerator] Font asset already exists at '{outputPath}'. Delete it first to regenerate.");
            return;
        }

        // Create the TMP Font Asset
        // Use Dynamic mode so it rasterizes glyphs on demand — supports ALL characters
        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            90,   // sampling point size
            9,    // padding
            UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
            1024, // atlas width
            1024  // atlas height
        );

        if (fontAsset == null)
        {
            Debug.LogError("[KoreanFontGenerator] Failed to create TMP font asset.");
            return;
        }

        fontAsset.name = outputName;

        // Set to Dynamic so Korean glyphs are generated at runtime as needed
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;

        // Save the asset
        AssetDatabase.CreateAsset(fontAsset, outputPath);

        // Save atlas texture
        if (fontAsset.atlasTexture != null)
        {
            fontAsset.atlasTexture.name = $"{outputName} Atlas";
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
        }

        // Save material
        if (fontAsset.material != null)
        {
            fontAsset.material.name = $"{outputName} Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[KoreanFontGenerator] Successfully created TMP font asset at '{outputPath}'.");
        Debug.Log("[KoreanFontGenerator] You can now assign this font to TMP components or set as the default TMP font.");

        // Ping the asset in Project window
        EditorGUIUtility.PingObject(fontAsset);
    }
}
