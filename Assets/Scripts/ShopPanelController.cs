using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Controls the shop panel: open/close with slide animation, tab switching, background overlay dismiss.
/// Attach to Canvas or a manager object.
/// </summary>
public class ShopPanelController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject upgradeScrollView;
    [SerializeField] private GameObject decoScrollView;

    [Header("Buttons")]
    [SerializeField] private Button shopButton;
    [SerializeField] private Button upgradeTabButton;
    [SerializeField] private Button decoTabButton;

    [Header("Background Overlay")]
    [SerializeField] private Button backgroundOverlay;

    [Header("Animation")]
    [Tooltip("Duration of the slide animation in seconds")]
    [SerializeField] private float slideDuration = 0.3f;

    private RectTransform shopPanelRect;
    private float openPositionX;   // anchoredPosition.x when fully visible
    private float closedPositionX; // anchoredPosition.x when off-screen (right)
    private Coroutine slideCoroutine;
    private bool isAnimating;
    private bool isShopOpen;

    private void Awake()
    {
        shopPanelRect = shopPanel.GetComponent<RectTransform>();

        // Store the designed open position
        openPositionX = shopPanelRect.anchoredPosition.x;

        // Closed position: slide the panel fully off-screen to the right
        // Panel width / 2 pushes the center past the right anchor
        closedPositionX = shopPanelRect.sizeDelta.x / 2f;
    }

    private void OnEnable()
    {
        shopButton.onClick.AddListener(OpenShop);
        upgradeTabButton.onClick.AddListener(ShowUpgradeTab);
        decoTabButton.onClick.AddListener(ShowDecoTab);
        backgroundOverlay.onClick.AddListener(CloseShop);
    }

    private void OnDisable()
    {
        shopButton.onClick.RemoveListener(OpenShop);
        upgradeTabButton.onClick.RemoveListener(ShowUpgradeTab);
        decoTabButton.onClick.RemoveListener(ShowDecoTab);
        backgroundOverlay.onClick.RemoveListener(CloseShop);
    }

    private void Start()
    {
        // Ensure shop is closed on start (no animation)
        shopPanel.SetActive(false);
        backgroundOverlay.gameObject.SetActive(false);
        // Set panel to closed position so first open animates correctly
        SetPanelPositionX(closedPositionX);
        isShopOpen = false;
    }

    /// <summary>Toggle shop panel open/closed. Safe to call from WebBridge.</summary>
    public void ToggleShop()
    {
        if (isShopOpen)
            CloseShop();
        else
            OpenShop();
    }

    public void OpenShop()
    {
        if (isAnimating) return;

        // Activate overlay first, then ensure correct z-order:
        // overlay behind → shopPanel on top
        backgroundOverlay.gameObject.SetActive(true);
        backgroundOverlay.transform.SetAsLastSibling();
        shopPanel.SetActive(true);
        shopPanel.transform.SetAsLastSibling();

        // Make sure panel starts at closed position
        SetPanelPositionX(closedPositionX);
        ShowUpgradeTab(); // Default tab

        // Slide from right to left (closedPositionX -> openPositionX)
        StartSlide(closedPositionX, openPositionX, false);
        isShopOpen = true;
    }

    public void CloseShop()
    {
        if (isAnimating) return;

        // Slide from left to right (openPositionX -> closedPositionX), then deactivate
        StartSlide(openPositionX, closedPositionX, true);
        isShopOpen = false;
    }

    public void ShowUpgradeTab()
    {
        upgradeScrollView.SetActive(true);
        decoScrollView.SetActive(false);
    }

    public void ShowDecoTab()
    {
        upgradeScrollView.SetActive(false);
        decoScrollView.SetActive(true);
    }

    // ─── Animation helpers ────────────────────────────────────────────

    private void StartSlide(float from, float to, bool deactivateOnComplete)
    {
        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);

        slideCoroutine = StartCoroutine(SlideCoroutine(from, to, deactivateOnComplete));
    }

    private IEnumerator SlideCoroutine(float from, float to, bool deactivateOnComplete)
    {
        isAnimating = true;
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);

            // Ease-out cubic for a smooth deceleration feel
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            SetPanelPositionX(Mathf.Lerp(from, to, eased));
            yield return null;
        }

        SetPanelPositionX(to);

        if (deactivateOnComplete)
        {
            shopPanel.SetActive(false);
            backgroundOverlay.gameObject.SetActive(false);
        }

        isAnimating = false;
        slideCoroutine = null;
    }

    private void SetPanelPositionX(float x)
    {
        Vector2 pos = shopPanelRect.anchoredPosition;
        pos.x = x;
        shopPanelRect.anchoredPosition = pos;
    }
}
