using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Discovers, registers, and tracks all individual LootCollectible items in the mission.
/// Prevents duplicate registrations and provides event-driven progress calculations.
/// </summary>
public class LootManager : MonoBehaviour
{
    public static LootManager Instance { get; private set; }

    [SerializeField] private List<LootCollectible> registeredLoot = new List<LootCollectible>();

    private readonly HashSet<LootCollectible> lootSet = new HashSet<LootCollectible>();
    private readonly HashSet<string> collectedIds = new HashSet<string>();

    public IReadOnlyList<LootCollectible> RegisteredLoot => registeredLoot;
    public int TotalLootCount => registeredLoot.Count;

    public int CollectedLootCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < registeredLoot.Count; i++)
                if (registeredLoot[i] != null && registeredLoot[i].IsCollected)
                    count++;
            return count;
        }
    }

    public int TotalPossibleValue
    {
        get
        {
            int sum = 0;
            for (int i = 0; i < registeredLoot.Count; i++)
                if (registeredLoot[i] != null)
                    sum += registeredLoot[i].MonetaryValue;
            return sum;
        }
    }

    public int CollectedLootValue
    {
        get
        {
            int sum = 0;
            for (int i = 0; i < registeredLoot.Count; i++)
                if (registeredLoot[i] != null && registeredLoot[i].IsCollected)
                    sum += registeredLoot[i].MonetaryValue;
            return sum;
        }
    }

    public int RequiredLootCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < registeredLoot.Count; i++)
                if (registeredLoot[i] != null && registeredLoot[i].RequiredForExit)
                    count++;
            return count;
        }
    }

    public int CollectedRequiredLootCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < registeredLoot.Count; i++)
                if (registeredLoot[i] != null && registeredLoot[i].RequiredForExit && registeredLoot[i].IsCollected)
                    count++;
            return count;
        }
    }

    public bool AreAllRequiredLootCollected => CollectedRequiredLootCount >= RequiredLootCount;
    public bool AreAllLootCollected => TotalLootCount > 0 && CollectedLootCount >= TotalLootCount;

    public float NormalizedProgress
    {
        get
        {
            if (TotalLootCount <= 0) return 1f;
            return Mathf.Clamp01((float)CollectedLootCount / TotalLootCount);
        }
    }

    public event Action<LootCollectible> OnLootRegistered;
    public event Action<LootCollectible> OnLootCollected;
    public event Action OnAllLootCollected;
    public event Action OnLootStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[LootManager] Duplicate LootManager found. Disabling duplicate.", this);
            enabled = false;
            return;
        }

        Instance = this;
        DiscoverSceneLoot();
    }

    private void Start()
    {
        DiscoverSceneLoot();
        OnLootStateChanged?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void DiscoverSceneLoot()
    {
        registeredLoot.RemoveAll(item => item == null);
        lootSet.Clear();
        for (int i = 0; i < registeredLoot.Count; i++)
            lootSet.Add(registeredLoot[i]);

        LootCollectible[] found = FindObjectsByType<LootCollectible>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < found.Length; i++)
            RegisterLoot(found[i]);
    }

    public void RegisterLoot(LootCollectible collectible)
    {
        if (collectible == null || lootSet.Contains(collectible)) return;

        lootSet.Add(collectible);
        if (!registeredLoot.Contains(collectible))
            registeredLoot.Add(collectible);

        OnLootRegistered?.Invoke(collectible);
        OnLootStateChanged?.Invoke();
    }

    public void NotifyLootCollected(LootCollectible collectible)
    {
        if (collectible == null) return;
        RegisterLoot(collectible);

        if (!collectedIds.Add(collectible.LootId))
            return;

        OnLootCollected?.Invoke(collectible);
        OnLootStateChanged?.Invoke();

        if (MissionManager.Instance != null)
            MissionManager.Instance.NotifyLootCollected(collectible);

        if (AreAllLootCollected)
        {
            OnAllLootCollected?.Invoke();
            if (MissionManager.Instance != null)
                MissionManager.Instance.NotifyAllLootCollected();
        }
    }

    public List<LootCollectible> GetRemainingRequiredLoot(bool requireAllLootGlobal)
    {
        List<LootCollectible> remaining = new List<LootCollectible>();
        for (int i = 0; i < registeredLoot.Count; i++)
        {
            LootCollectible item = registeredLoot[i];
            if (item == null || item.IsCollected) continue;
            if (requireAllLootGlobal || item.RequiredForExit)
                remaining.Add(item);
        }
        return remaining;
    }
}
