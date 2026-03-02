using UnityEngine;

// ──────────────────────────────────────────────────────────────
// WebBridge — JavaScript ↔ Unity WebGL communication gateway.
//
// Responsibilities:
//   • Receives calls from JavaScript via SendMessage("WebBridge", ...)
//   • Routes to the appropriate singleton manager
//   • All methods accept string parameters (WebGL SendMessage limitation)
//   • Ensures GameObject is named "WebBridge" for JS interop
// ──────────────────────────────────────────────────────────────
public class WebBridge : MonoBehaviour
{
    private void Awake()
    {
        // Force GameObject name so JS can find it via SendMessage
        gameObject.name = "WebBridge";
    }

    // ─── Public API (called from JavaScript) ──────────────────

    /// <summary>
    /// Add points from an external web source.
    /// JS usage: unityInstance.SendMessage("WebBridge", "AddPointsFromWeb", "100");
    /// </summary>
    public void AddPointsFromWeb(string amountStr)
    {
        if (!int.TryParse(amountStr, out int amount))
        {
            Debug.LogWarning($"[WebBridge] AddPointsFromWeb: invalid amount '{amountStr}'.");
            return;
        }

        if (SweetPointManager.Instance == null)
        {
            Debug.LogWarning("[WebBridge] AddPointsFromWeb: SweetPointManager.Instance is null.");
            return;
        }

        SweetPointManager.Instance.AddPoints(amount);
        Debug.Log($"[WebBridge] Added {amount} points from web.");
    }

    /// <summary>
    /// Toggle the shop panel open/closed from web.
    /// JS usage: unityInstance.SendMessage("WebBridge", "ToggleShopFromWeb");
    /// </summary>
    public void ToggleShopFromWeb()
    {
        var shop = FindAnyObjectByType<ShopPanelController>();
        if (shop == null)
        {
            Debug.LogWarning("[WebBridge] ToggleShopFromWeb: ShopPanelController not found in scene.");
            return;
        }

        shop.ToggleShop();
        Debug.Log("[WebBridge] Toggled shop from web.");
    }

    /// <summary>
    /// Start placing a decoration from web.
    /// JS usage: unityInstance.SendMessage("WebBridge", "StartPlacingFromWeb", "sticker01");
    /// </summary>
    public void StartPlacingFromWeb(string itemId)
    {
        if (DecorationManager.Instance == null)
        {
            Debug.LogWarning("[WebBridge] StartPlacingFromWeb: DecorationManager.Instance is null.");
            return;
        }

        DecorationManager.Instance.StartPlacing(itemId);
        Debug.Log($"[WebBridge] Started placing '{itemId}' from web.");
    }
}
