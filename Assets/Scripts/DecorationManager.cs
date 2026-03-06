using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

// ──────────────────────────────────────────────────────────────
// Data class: maps an itemId to a prefab for purchase & placement
// ──────────────────────────────────────────────────────────────
[Serializable]
public class PlaceableItemDefinition
{
    public string itemId;
    public string displayName;
    public int cost = 15;
    [Tooltip("Prefab to instantiate for world placement")]
    public GameObject placementPrefab;
    [Tooltip("Sprite shown in the DecoImage area of the shop slot")]
    public Sprite decoSprite;
}

// ──────────────────────────────────────────────────────────────
// Runtime binding: links one PlaceableItemDefinition to its shop UI instance
// ──────────────────────────────────────────────────────────────
internal class DecoSlotBinding
{
    public PlaceableItemDefinition def;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public Button buyButton;
}

// ──────────────────────────────────────────────────────────────
// DecorationManager — shop UI for deco items + free placement.
//
// Responsibilities:
//   • Spawns DecoItem prefab instances into the Deco scroll content
//   • BuyButton click → TrySpend → close shop → StartPlacing
//   • StartPlacing: prefab follows mouse, left-click confirm, right/ESC cancel
//   • Handles overlap via auto-incrementing SpriteRenderer.sortingOrder
// ──────────────────────────────────────────────────────────────
public class DecorationManager : MonoBehaviour
{
    public static DecorationManager Instance { get; private set; }

    [Header("Deco Shop Items")]
    [SerializeField] private List<PlaceableItemDefinition> placeableItems = new List<PlaceableItemDefinition>();

    [Header("Shop UI")]
    [Tooltip("DecoItem prefab with DecoName, Cost (TMP) and BuyButton")]
    [SerializeField] private GameObject decoItemPrefab;
    [Tooltip("Content transform inside the Deco ScrollView")]
    [SerializeField] private Transform decoContentParent;

    [Header("Font")]
    [Tooltip("Korean TMP Font Asset")]
    [SerializeField] private TMP_FontAsset koreanFont;

    [Header("Placement Settings")]
    [Tooltip("Camera used for screen-to-world conversion. If null, Camera.main is used.")]
    [SerializeField] private Camera placementCamera;
    [Tooltip("Z depth for placed decorations")]
    [SerializeField] private float placementZ = 0f;

    // runtime — shop
    private readonly List<DecoSlotBinding> bindings = new List<DecoSlotBinding>();

    // runtime — placement
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

    private void Start()
    {
        SpawnAllSlots();
        RefreshAllUI();
    }

    private void Update()
    {
        if (!isPlacing || currentPreview == null) return;

        // New Input System: Use Pointer instead of Mouse for cross-platform (Touch/Pen/Mouse)
        var pointer = Pointer.current;
        var keyboard = Keyboard.current;
        
        if (pointer == null) return;

        Camera cam = placementCamera != null ? placementCamera : Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[DecorationManager] No camera available for placement.");
            CancelPlacing();
            return;
        }

        // 1. Follow pointer (Mouse hover OR Touch drag)
        Vector3 worldPos = cam.ScreenToWorldPoint(pointer.position.ReadValue());
        worldPos.z = placementZ;
        currentPreview.transform.position = worldPos;

        // 2. Mobile Touch Logic: Place on finger release, cancel on multi-touch
        if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
        {
            var activeTouch = Touchscreen.current.primaryTouch;
            
            // If they lift their finger from the screen -> Confirm Placement
            if (activeTouch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Ended ||
                activeTouch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                ConfirmPlacement();
                return;
            }
            // Cancel if they use a second finger
            if (Touchscreen.current.touches.Count > 1)
            {
                CancelPlacing();
                return;
            }
        }
        // 3. Desktop Mouse Logic: Left click to place, Right click/Esc to cancel
        else
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    ConfirmPlacement();
                }
                else if (mouse.rightButton.wasPressedThisFrame ||
                         (keyboard != null && keyboard.escapeKey.wasPressedThisFrame))
                {
                    CancelPlacing();
                }
            }
        }
    }

    // ─── Public API ───────────────────────────────────────────

    /// <summary>
    /// Purchase a deco item: deduct points, close shop, start placement.
    /// </summary>
    public bool TryBuyAndPlace(string itemId)
    {
        PlaceableItemDefinition def = FindDefinition(itemId);
        if (def == null)
        {
            Debug.LogWarning($"[DecorationManager] Item '{itemId}' not found.");
            return false;
        }

        if (SweetPointManager.Instance == null)
        {
            Debug.LogWarning("[DecorationManager] SweetPointManager.Instance is null.");
            return false;
        }

        if (!SweetPointManager.Instance.TrySpend(def.cost))
        {
            Debug.Log($"[DecorationManager] Not enough points to buy '{itemId}' (cost: {def.cost}).");
            return false;
        }

        Debug.Log($"[DecorationManager] Bought '{itemId}' for {def.cost} points.");

        // Close shop before starting placement
        var shop = FindAnyObjectByType<ShopPanelController>();
        if (shop != null) shop.CloseShop();

        // Start placement
        StartPlacing(itemId);
        return true;
    }

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
        if (def.placementPrefab == null)
        {
            Debug.LogWarning($"[DecorationManager] Placement prefab for '{itemId}' is null. Purchase recorded but no placement.");
            return;
        }

        currentPreview = Instantiate(def.placementPrefab);
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

    // ─── Shop UI ──────────────────────────────────────────────

    private void SpawnAllSlots()
    {
        if (decoItemPrefab == null)
        {
            Debug.LogWarning("[DecorationManager] decoItemPrefab is not assigned.");
            return;
        }
        if (decoContentParent == null)
        {
            Debug.LogWarning("[DecorationManager] decoContentParent is not assigned.");
            return;
        }

        // Sort by cost ascending so cheapest items appear first
        var sorted = placeableItems.OrderBy(d => d.cost).ToList();

        foreach (var def in sorted)
            SpawnSlot(def);
    }

    private void SpawnSlot(PlaceableItemDefinition def)
    {
        GameObject go = Instantiate(decoItemPrefab, decoContentParent);

        var binding = new DecoSlotBinding { def = def };

        // Set DecoImage sprite if available
        Transform decoImgT = go.transform.Find("DecoImage");
        if (decoImgT != null && def.decoSprite != null)
        {
            Image decoImg = decoImgT.GetComponent<Image>();
            if (decoImg != null)
            {
                decoImg.sprite = def.decoSprite;
                decoImg.preserveAspect = true;
            }
        }

        // Resolve child references from the DecoItem prefab structure
        Transform hw = go.transform.Find("HorizontalWrapper");
        if (hw != null)
        {
            Transform nameT = hw.Find("DecoName");
            if (nameT != null) binding.nameText = nameT.GetComponent<TextMeshProUGUI>();

            Transform costT = hw.Find("Cost");
            if (costT != null) binding.costText = costT.GetComponent<TextMeshProUGUI>();
        }

        Transform buyT = go.transform.Find("BuyButton");
        if (buyT != null)
        {
            binding.buyButton = buyT.GetComponent<Button>();
            if (binding.buyButton != null)
            {
                string capturedId = def.itemId;
                binding.buyButton.onClick.AddListener(() => TryBuyAndPlace(capturedId));
            }

            // Apply font to buy button text
            Transform buyTextT = buyT.Find("Text (TMP)");
            if (buyTextT != null) ApplyFont(buyTextT.GetComponent<TextMeshProUGUI>());
        }

        // Apply Korean font
        ApplyFont(binding.nameText);
        ApplyFont(binding.costText);

        bindings.Add(binding);
    }

    private void RefreshAllUI()
    {
        foreach (var b in bindings)
        {
            if (b.nameText != null)
                b.nameText.text = b.def.displayName;

            if (b.costText != null)
                b.costText.text = $"{b.def.cost}";
        }
    }

    // ─── Placement Internal ───────────────────────────────────

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

    private void ApplyFont(TextMeshProUGUI tmp)
    {
        if (tmp != null && koreanFont != null)
            tmp.font = koreanFont;
    }
}
