using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to JellyButton. Each click adds sweet points.
/// Uses UpgradeManager.GetPointsPerClick() if available, otherwise falls back to 1.
/// </summary>
[RequireComponent(typeof(Button))]
public class JellyClickHandler : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        button.onClick.AddListener(OnClick);
    }

    private void OnDisable()
    {
        button.onClick.RemoveListener(OnClick);
    }

    private void OnClick()
    {
        if (SweetPointManager.Instance == null)
        {
            Debug.LogWarning("[JellyClickHandler] SweetPointManager.Instance is null.");
            return;
        }

        // Use UpgradeManager for points-per-click; fallback to 1
        int points = 1;
        if (UpgradeManager.Instance != null)
            points = UpgradeManager.Instance.GetPointsPerClick();

        SweetPointManager.Instance.AddPoints(points);
    }
}
