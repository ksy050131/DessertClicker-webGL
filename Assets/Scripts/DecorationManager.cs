using System;
using System.Collections.Generic;
using UnityEngine;

// ──────────────────────────────────────────────────────────────
// Data class: maps an itemId to a prefab for free placement
// ──────────────────────────────────────────────────────────────
[Serializable]
public class PlaceableItemDefinition
{
    public string itemId;
    public GameObject prefab;
}

// ──────────────────────────────────────────────────────────────
// DecorationManager — free placement of sticker prefabs.
//
// Responsibilities:
//   • Manages a list of placeable item definitions (Inspector)
//   • StartPlacing(itemId) spawns a prefab that follows the mouse
//   • Left-click confirms placement, right-click / ESC cancels
//   • Handles overlap via auto-incrementing SpriteRenderer.sortingOrder
//   • Desktop-only (mouse input)
// ──────────────────────────────────────────────────────────────
public class DecorationManager : MonoBehaviour
{
    public static DecorationManager Instance { get; private set; }

    [Header("Placeable Items")]
    [SerializeField] private List<PlaceableItemDefinition> placeableItems = new List<PlaceableItemDefinition>();

    [Header("Placement Settings")]
    [Tooltip("Camera used for screen-to-world conversion. If null, Camera.main is used.")]
    [SerializeField] private Camera placementCamera;
    [Tooltip("Z depth for placed decorations")]
    [SerializeField] private float placementZ = 0f;

    // runtime
    private GameObject currentPreview;
    private bool isPlacing;
    private int nextSortingOrder = 1;

    // ─── Lifecycle ────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (!isPlacing || currentPreview == null) return;

        // Follow mouse
        Camera cam = placementCamera != null ? placementCamera : Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[DecorationManager] No camera available for placement.");
            CancelPlacing();
            return;
        }

        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = placementZ;
        currentPreview.transform.position = mouseWorld;

        // Left-click → confirm placement
        if (Input.GetMouseButtonDown(0))
        {
            ConfirmPlacement();
        }
        // Right-click or ESC → cancel
        else if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacing();
        }
    }

    // ─── Public API ───────────────────────────────────────────

    /// <summary>
    /// Begin placing a decoration. The prefab follows the mouse
    /// until the player left-clicks (confirm) or right-clicks/ESC (cancel).
    /// </summary>
    public void StartPlacing(string itemId)
    {
        if (isPlacing)
        {
            Debug.LogWarning("[DecorationManager] Already placing an item. Cancel or confirm first.");
            return;
        }

        PlaceableItemDefinition def = FindDefinition(itemId);
        if (def == null)
        {
            Debug.LogWarning($"[DecorationManager] Item '{itemId}' not found in placeableItems list.");
            return;
        }
        if (def.prefab == null)
        {
            Debug.LogWarning($"[DecorationManager] Prefab for '{itemId}' is null.");
            return;
        }

        currentPreview = Instantiate(def.prefab);
        currentPreview.name = $"Deco_{itemId}_{nextSortingOrder}";

        // Set sorting order for overlap handling
        var sr = currentPreview.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = nextSortingOrder;
        }

        // Also set Canvas sorting if it's a UI element
        var canvas = currentPreview.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.sortingOrder = nextSortingOrder;
        }

        isPlacing = true;
        Debug.Log($"[DecorationManager] Started placing '{itemId}'. Left-click to confirm, right-click/ESC to cancel.");
    }

    // ─── Internal ─────────────────────────────────────────────

    private void ConfirmPlacement()
    {
        if (currentPreview == null) return;

        nextSortingOrder++;
        Debug.Log($"[DecorationManager] Placed '{currentPreview.name}' at {currentPreview.transform.position}.");

        currentPreview = null;
        isPlacing = false;
    }

    private void CancelPlacing()
    {
        if (currentPreview != null)
        {
            Debug.Log($"[DecorationManager] Cancelled placing '{currentPreview.name}'.");
            Destroy(currentPreview);
            currentPreview = null;
        }
        isPlacing = false;
    }

    private PlaceableItemDefinition FindDefinition(string itemId)
    {
        foreach (var def in placeableItems)
            if (def.itemId == itemId) return def;
        return null;
    }
}
