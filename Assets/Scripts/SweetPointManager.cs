using System;
using UnityEngine;
using TMPro;

/// <summary>
/// Singleton manager for Sweet Points currency.
/// Attach to any persistent GameObject (e.g. Canvas).
/// </summary>
public class SweetPointManager : MonoBehaviour
{
    public static SweetPointManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI scoreText;

    /// <summary>Fires whenever the point total changes. Arg = new total.</summary>
    public event Action<int> OnPointsChanged;

    private int currentPoints;
    public int CurrentPoints => currentPoints;

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
        UpdateUI();
    }

    /// <summary>Add points (e.g. from clicking the jelly).</summary>
    public void AddPoints(int amount)
    {
        if (amount <= 0) return;
        currentPoints += amount;
        OnPointsChanged?.Invoke(currentPoints);
        UpdateUI();
    }

    /// <summary>Try to spend points. Returns true if successful.</summary>
    public bool TrySpend(int amount)
    {
        if (amount <= 0 || currentPoints < amount) return false;
        currentPoints -= amount;
        OnPointsChanged?.Invoke(currentPoints);
        UpdateUI();
        return true;
    }

    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"Sweet Points {currentPoints}";
    }
}
