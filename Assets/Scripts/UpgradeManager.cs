using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ──────────────────────────────────────────────────────────────
// Data class: one upgrade definition (click or auto)
// ──────────────────────────────────────────────────────────────
[Serializable]
public class UpgradeDefinition
{
    public string id;
    public string displayName;
    [TextArea(1, 2)]
    public string description;
    public int baseCost = 10;
    [Tooltip("Cost multiplier per level (e.g. 1.15 = 15% increase)")]
    public float costMultiplier = 1.15f;
    [Tooltip("Value added per level (points-per-click or auto-per-second)")]
    public float valuePerLevel = 1f;

    [Header("Critical Hit (Optional)")]
    [Tooltip("If true, valuePerLevel is added to baseCriticalChance per level. (e.g., 0.01 = 1%)")]
    public bool isCriticalUpgrade = false;
    public float baseCriticalChance = 0.05f; // 5%
    public int criticalMultiplier = 10;

    [HideInInspector] public int currentLevel;

    /// <summary>Current cost rounded to int.</summary>
    public int GetCurrentCost()
    {
        return Mathf.RoundToInt(baseCost * Mathf.Pow(costMultiplier, currentLevel));
    }

    /// <summary>Total value at current level.</summary>
    public float GetTotalValue()
    {
        return valuePerLevel * currentLevel;
    }
}

// ──────────────────────────────────────────────────────────────
// Runtime binding: links one UpgradeDefinition to its UI prefab instance
// ──────────────────────────────────────────────────────────────
internal class UpgradeSlotBinding
{
    public UpgradeDefinition def;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI contentText;
    public TextMeshProUGUI costText;
    public Button buyButton;
}

// ──────────────────────────────────────────────────────────────
// UpgradeManager — handles click and auto upgrades.
//
// Responsibilities:
//   • Maintains lists of click-upgrade and auto-upgrade definitions
//   • Spawns UpgradeItem prefab instances into the upgrade scroll content
//   • Processes purchases via SweetPointManager.TrySpend()
//   • Provides GetPointsPerClick() and GetAutoPointsPerSecond()
//   • Accumulates auto-points in Update() (FPS-independent)
// ──────────────────────────────────────────────────────────────
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("Upgrade Definitions")]
    [SerializeField] private List<UpgradeDefinition> clickUpgrades = new List<UpgradeDefinition>();
    [SerializeField] private List<UpgradeDefinition> autoUpgrades = new List<UpgradeDefinition>();

    [Header("UI")]
    [Tooltip("Prefab with UpgradeName, UpgradeContent, Cost (TMP) and BuyButton")]
    [SerializeField] private GameObject upgradeItemPrefab;
    [Tooltip("Content transform inside the Upgrade ScrollView")]
    [SerializeField] private Transform upgradeContentParent;

    [Header("Font")]
    [Tooltip("Korean TMP Font Asset (e.g. Galmuri11 SDF). Applied to all spawned upgrade slots.")]
    [SerializeField] private TMP_FontAsset koreanFont;

    [Header("Settings")]
    [Tooltip("Base points per click before upgrades")]
    [SerializeField] private int basePointsPerClick = 1;

    // runtime
    private readonly List<UpgradeSlotBinding> bindings = new List<UpgradeSlotBinding>();
    private float autoAccumulator;

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
        float autoRate = GetAutoPointsPerSecond();
        if (autoRate <= 0f) return;

        autoAccumulator += autoRate * Time.deltaTime;

        if (autoAccumulator >= 1f)
        {
            int points = Mathf.FloorToInt(autoAccumulator);
            autoAccumulator -= points;

            if (SweetPointManager.Instance != null)
            {
                SweetPointManager.Instance.AddPoints(points);
            }
            else
            {
                Debug.LogWarning("[UpgradeManager] SweetPointManager.Instance is null — cannot add auto-points.");
            }
        }
    }

    // ─── Public API ───────────────────────────────────────────

    /// <summary>Total points earned per click (base + upgrade sum). Also calculates if it was a critical hit.</summary>
    public int CalculateClickPoints(out bool isCritical)
    {
        isCritical = false;
        float bonus = 0f;
        float critChance = 0f;
        int critMult = 1;

        foreach (var def in clickUpgrades)
        {
            if (def.isCriticalUpgrade && def.currentLevel > 0)
            {
                critChance += def.baseCriticalChance + ((def.currentLevel - 1) * def.valuePerLevel);
                critMult = Mathf.Max(critMult, def.criticalMultiplier);
            }
            else if (!def.isCriticalUpgrade)
            {
                bonus += def.GetTotalValue();
            }
        }

        int totalClick = basePointsPerClick + Mathf.RoundToInt(bonus);

        if (critChance > 0f && UnityEngine.Random.value <= critChance)
        {
            isCritical = true;
            totalClick *= critMult;
        }

        return totalClick;
    }

    /// <summary>Total auto-points per second from all auto upgrades.</summary>
    public float GetAutoPointsPerSecond()
    {
        float total = 0f;
        foreach (var def in autoUpgrades)
            total += def.GetTotalValue();

        return total;
    }

    /// <summary>
    /// Attempt to buy an upgrade by id.
    /// Returns true if purchase succeeded.
    /// </summary>
    public bool TryBuy(string upgradeId)
    {
        UpgradeDefinition target = FindDefinition(upgradeId);
        if (target == null)
        {
            Debug.LogWarning($"[UpgradeManager] Upgrade '{upgradeId}' not found.");
            return false;
        }

        int cost = target.GetCurrentCost();

        if (SweetPointManager.Instance == null)
        {
            Debug.LogWarning("[UpgradeManager] SweetPointManager.Instance is null.");
            return false;
        }

        if (!SweetPointManager.Instance.TrySpend(cost))
        {
            Debug.Log($"[UpgradeManager] Not enough points to buy '{upgradeId}' (cost: {cost}).");
            return false;
        }

        target.currentLevel++;
        Debug.Log($"[UpgradeManager] Bought '{upgradeId}' → Lv{target.currentLevel} (cost was {cost}).");
        RefreshAllUI();
        return true;
    }

    // ─── Internal ─────────────────────────────────────────────

    private UpgradeDefinition FindDefinition(string id)
    {
        foreach (var def in clickUpgrades)
            if (def.id == id) return def;
        foreach (var def in autoUpgrades)
            if (def.id == id) return def;
        return null;
    }

    private void SpawnAllSlots()
    {
        if (upgradeItemPrefab == null)
        {
            Debug.LogWarning("[UpgradeManager] upgradeItemPrefab is not assigned.");
            return;
        }
        if (upgradeContentParent == null)
        {
            Debug.LogWarning("[UpgradeManager] upgradeContentParent is not assigned.");
            return;
        }

        // Combine both lists and sort them by current cost
        var combinedUpgrades = new List<UpgradeDefinition>();
        combinedUpgrades.AddRange(clickUpgrades);
        combinedUpgrades.AddRange(autoUpgrades);

        var sortedUpgrades = combinedUpgrades.OrderBy(u => u.GetCurrentCost()).ToList();

        foreach (var def in sortedUpgrades)
            SpawnSlot(def);
    }

    private void SpawnSlot(UpgradeDefinition def)
    {
        GameObject go = Instantiate(upgradeItemPrefab, upgradeContentParent);

        var binding = new UpgradeSlotBinding { def = def };

        // Resolve child references from the UpgradeItem prefab structure
        Transform vw = go.transform.Find("VerticalWrapper");
        if (vw != null)
        {
            Transform nameT = vw.Find("UpgradeName");
            if (nameT != null) binding.nameText = nameT.GetComponent<TextMeshProUGUI>();

            Transform contentT = vw.Find("UpgradeContent");
            if (contentT != null) binding.contentText = contentT.GetComponent<TextMeshProUGUI>();

            Transform costT = vw.Find("Cost");
            if (costT != null) binding.costText = costT.GetComponent<TextMeshProUGUI>();
        }

        Transform buyT = go.transform.Find("BuyButton");
        if (buyT != null)
        {
            binding.buyButton = buyT.GetComponent<Button>();
            if (binding.buyButton != null)
            {
                string capturedId = def.id; // capture for closure
                binding.buyButton.onClick.AddListener(() => TryBuy(capturedId));
            }

            // Apply font to buy button text too
            Transform buyTextT = buyT.Find("Text (TMP)");
            if (buyTextT != null) ApplyFont(buyTextT.GetComponent<TextMeshProUGUI>());
        }

        // Apply Korean font to all text fields
        ApplyFont(binding.nameText);
        ApplyFont(binding.contentText);
        ApplyFont(binding.costText);

        bindings.Add(binding);
    }

    private void RefreshAllUI()
    {
        foreach (var b in bindings)
        {
            if (b.nameText != null)
                b.nameText.text = $"{b.def.displayName} Lv.{b.def.currentLevel}";

            if (b.contentText != null)
                b.contentText.text = string.IsNullOrEmpty(b.def.description)
                    ? ""
                    : b.def.description;

            if (b.costText != null)
                b.costText.text = $"{b.def.GetCurrentCost()}";
        }
    }

    private void ApplyFont(TextMeshProUGUI tmp)
    {
        if (tmp != null && koreanFont != null)
            tmp.font = koreanFont;
    }
}
