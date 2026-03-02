using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to create a sticker placement prefab from a Sprite.
/// Run via menu: Tools > Create Sticker Prefab From Jelly
/// </summary>
public class StickerPrefabCreator : EditorWindow
{
    [MenuItem("Tools/Create Sticker Prefab From Jelly")]
    public static void Create()
    {
        string spritePath = "Assets/Arts/Jelly.png";
        string prefabFolder = "Assets/Prefabs";
        string prefabPath = $"{prefabFolder}/StickerJelly.prefab";

        // Set texture import settings to Sprite
        TextureImporter importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
        if (importer != null)
        {
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 100;
                importer.SaveAndReimport();
                Debug.Log("[StickerPrefabCreator] Set Jelly.png texture type to Sprite.");
            }
        }
        else
        {
            Debug.LogError($"[StickerPrefabCreator] Could not find texture at '{spritePath}'.");
            return;
        }

        // Load the sprite
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null)
        {
            Debug.LogError("[StickerPrefabCreator] Failed to load Sprite from Jelly.png.");
            return;
        }

        // Create a temporary GameObject
        GameObject go = new GameObject("StickerJelly");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 0;

        // Scale down to a reasonable sticker size
        go.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

        // Save as prefab
        if (!AssetDatabase.IsValidFolder(prefabFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);

        if (prefab != null)
        {
            Debug.Log($"[StickerPrefabCreator] Created sticker prefab at '{prefabPath}'.");
            EditorGUIUtility.PingObject(prefab);
        }
        else
        {
            Debug.LogError("[StickerPrefabCreator] Failed to create prefab.");
        }
    }
}
