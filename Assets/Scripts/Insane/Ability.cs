using System;
using System.Collections.Generic;
using UnityEngine;

public class Ability : MonoBehaviour
{
    public const string DefaultFirstAbilityName  = "기본공격";
    public const string DefaultSecondAbilityName = "전장이동";

    [Header("References")]
    [SerializeField] private InsaneManager      insaneManager;
    [Tooltip("AbilityItem들의 부모 Transform")]
    [SerializeField] private Transform          itemContainer;
    [SerializeField] private AbilitySelectWindow abilitySelectWindow;

    private readonly List<AbilityItem> abilityItems = new List<AbilityItem>();
    private bool isApplyingSaveData = false;

    // ─── 초기화 ──────────────────────────────────────────────

    private void Awake()
    {
        //EnsureInsaneManager();
        CollectItemsFromContainer();
    }

    private void Start()
    {
        RefreshAllItems();
        ApplyAbilitySaveData(insaneManager != null && insaneManager.CurrentSheet != null
            ? insaneManager.CurrentSheet.ability
            : null);
        RefreshAllItems();
        SyncAbilityDataToManager();

        // 창이 처음 열리기 전에 미리 패널을 구성합니다 (ContentSizeFitter 정상 동작)
        InitializeSelectWindow();
    }

    private void InitializeSelectWindow()
    {
        if (abilitySelectWindow == null || insaneManager == null)
            return;

        abilitySelectWindow.Initialize(
            insaneManager.GetOfficialAbilities(),
            insaneManager.GetUnofficialAbilities(),
            insaneManager.GetIshikiAbilities());
    }

    // ─── 외부 호출 ────────────────────────────────────────────

    public void SetInsaneManager(InsaneManager manager)
    {
        if (manager != null)
            insaneManager = manager;
    }

    /// <summary>
    /// 어빌리티 데이터베이스나 특기 이름이 변경되었을 때 외부에서 호출합니다.
    /// </summary>
    public void RefreshAllItems()
    {
        EnsureInsaneManager();

        if (insaneManager == null)
        {
            Debug.LogWarning("[Ability] insaneManager가 null — 인스펙터 미할당");
            return;
        }

        List<AbilityData>                allAbilities      = insaneManager.GetAllAbilities();
        Dictionary<string, List<string>> specialtiesByArea = insaneManager.GetSpecialtiesByArea();
        Dictionary<string, int>          usedCounts        = CollectUsedAbilityCounts();

        for (int i = 0; i < abilityItems.Count; i++)
            SetupItem(abilityItems[i], allAbilities, specialtiesByArea, usedCounts);
    }

    // ─── 선택 변경 콜백 ───────────────────────────────────────

    private void OnItemSelectionChanged(AbilityItem changedItem)
    {
        if (isApplyingSaveData || insaneManager == null)
            return;

        List<AbilityData>                allAbilities      = insaneManager.GetAllAbilities();
        Dictionary<string, List<string>> specialtiesByArea = insaneManager.GetSpecialtiesByArea();
        Dictionary<string, int>          usedCounts        = CollectUsedAbilityCounts();

        for (int i = 0; i < abilityItems.Count; i++)
        {
            AbilityItem item = abilityItems[i];
            if (item == null || item == changedItem)
                continue;

            SetupItem(item, allAbilities, specialtiesByArea, usedCounts);
        }

        SyncAbilityDataToManager();
        ApplyAbilityStatEffects();
        ApplyAbilitySpecialEffects();
    }

    // ─── 창 열기 ─────────────────────────────────────────────

    private void OpenWindowForItem(AbilityItem item)
    {
        EnsureInsaneManager();

        if (insaneManager == null)
        {
            Debug.LogWarning("[Ability] insaneManager가 null — 창을 열 수 없습니다.");
            return;
        }

        if (abilitySelectWindow == null)
        {
            Debug.LogWarning("[Ability] abilitySelectWindow가 null — 인스펙터 미할당");
            return;
        }

        // 기본 어빌리티(index 0·1) 제외, 빈 슬롯에서 선택 제한 수에 도달하면 창 열기 차단
        int itemIndex = abilityItems.IndexOf(item);
        if (itemIndex >= 2 && string.IsNullOrEmpty(item.SelectedAbilityName))
        {
            if (CountSelectedNonDefaultAbilities() >= insaneManager.GetTotalAbilityCount())
                return;
        }

        Dictionary<string, int> usedCounts = CollectUsedAbilityCounts();
        List<AbilityData>       available  = FilterAbilities(
            insaneManager.GetAllAbilities(), usedCounts, item.SelectedAbilityName);

        // ── [DEBUG 6] 창 열기 직전 확인 ───────────────────────
        Debug.Log($"[Ability] OpenWindowForItem: item={item.name}, available={available.Count}개");

        string currentName = item.SelectedAbilityName;

        abilitySelectWindow.Open(
            available,
            selectedData =>
            {
                // ── [DEBUG 7] 선택 결과 수신 확인 ─────────────
                Debug.Log($"[Ability] 창 선택 콜백 수신: selectedData={selectedData?.name}");

                // 현재 선택된 항목을 다시 클릭하면 제거, 아니면 선택
                if (!string.IsNullOrEmpty(currentName) && selectedData?.name == currentName)
                    item.SelectAbility(null);
                else
                    item.SelectAbility(selectedData);
            },
            currentName);
        // item.SelectAbility 내부에서 onSelectionChanged → OnItemSelectionChanged 가 호출됩니다.
    }

    // ─── 내부 헬퍼 ───────────────────────────────────────────

    private void CollectItemsFromContainer()
    {
        abilityItems.Clear();

        if (itemContainer == null)
            return;

        for (int i = 0; i < itemContainer.childCount; i++)
        {
            AbilityItem item = itemContainer.GetChild(i).GetComponent<AbilityItem>();
            if (item != null)
                abilityItems.Add(item);
        }
    }

    private void SetupItem(
        AbilityItem                      item,
        List<AbilityData>                allAbilities,
        Dictionary<string, List<string>> specialtiesByArea,
        Dictionary<string, int>          usedCounts)
    {
        if (item == null)
            return;

        List<AbilityData> filtered = FilterAbilities(allAbilities, usedCounts, item.SelectedAbilityName);
        item.Setup(filtered, specialtiesByArea, OnItemSelectionChanged, OpenWindowForItem);
    }

    /// <summary>
    /// 획득 제한에 걸린 어빌리티를 목록에서 제외합니다.
    /// - Disallowed : 다른 슬롯에서 1개라도 선택 중이면 제외
    /// - Unlimited  : 항상 표시
    /// - Limited    : 다른 슬롯 선택 수가 maxAcquireCount 이상이면 제외
    /// 현재 슬롯 자신의 기여분(1)은 카운트에서 제외하여 비교합니다.
    /// </summary>
    private List<AbilityData> FilterAbilities(
        List<AbilityData>       all,
        Dictionary<string, int> usedCounts,
        string                  currentItemSelectedName)
    {
        List<AbilityData> result = new List<AbilityData>();

        for (int i = 0; i < all.Count; i++)
        {
            AbilityData data = all[i];

            if (data.acquireMode == AcquireMultipleMode.Unlimited)
            {
                result.Add(data);
                continue;
            }

            usedCounts.TryGetValue(data.name, out int totalCount);
            int othersCount = (data.name == currentItemSelectedName)
                ? totalCount - 1
                : totalCount;

            bool blocked = data.acquireMode == AcquireMultipleMode.Disallowed
                ? othersCount >= 1
                : othersCount >= data.maxAcquireCount; // Limited

            if (!blocked)
                result.Add(data);
        }

        return result;
    }

    private Dictionary<string, int> CollectUsedAbilityCounts()
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();

        for (int i = 0; i < abilityItems.Count; i++)
        {
            AbilityItem item = abilityItems[i];
            if (item == null) continue;

            string name = item.SelectedAbilityName;
            if (string.IsNullOrEmpty(name)) continue;

            if (counts.ContainsKey(name)) counts[name]++;
            else counts[name] = 1;
        }

        return counts;
    }

    /// <summary>기본 어빌리티(index 0·1) 제외, 선택된 어빌리티 수를 반환합니다.</summary>
    private int CountSelectedNonDefaultAbilities()
    {
        int count = 0;
        for (int i = 2; i < abilityItems.Count; i++)
        {
            if (abilityItems[i] != null && !string.IsNullOrEmpty(abilityItems[i].SelectedAbilityName))
                count++;
        }
        return count;
    }

    // ─── 저장 / 불러오기 ─────────────────────────────────────

    public InsaneAbilityData CreateAbilitySaveData()
    {
        InsaneAbilityData saveData = new InsaneAbilityData
        {
            acquiredAbilityNames = new List<string>(),
            abilities            = new List<InsaneAbilityEntryData>()
        };

        for (int i = 0; i < abilityItems.Count; i++)
        {
            AbilityItem item          = abilityItems[i];
            string      abilityName   = item != null ? item.SelectedAbilityName   : string.Empty;
            string      specialtyName = item != null ? item.SelectedSpecialtyName : string.Empty;

            saveData.abilities.Add(new InsaneAbilityEntryData
            {
                abilityName             = abilityName,
                designatedSpecialtyName = specialtyName
            });

            if (!string.IsNullOrEmpty(abilityName))
                saveData.acquiredAbilityNames.Add(abilityName);
        }

        return saveData;
    }

    public void ApplyAbilitySaveData(InsaneAbilityData saveData)
    {
        EnsureInsaneManager();
        isApplyingSaveData = true;

        List<AbilityData>                allAbilities      = insaneManager != null ? insaneManager.GetAllAbilities()      : new List<AbilityData>();
        Dictionary<string, List<string>> specialtiesByArea = insaneManager != null ? insaneManager.GetSpecialtiesByArea() : new Dictionary<string, List<string>>();
        List<InsaneAbilityEntryData>     entries           = BuildApplyEntries(saveData);

        for (int i = 0; i < abilityItems.Count; i++)
        {
            AbilityItem item = abilityItems[i];
            if (item == null) continue;

            item.SetAbilitySelectionLocked(false);

            InsaneAbilityEntryData entry       = i < entries.Count ? entries[i] : null;
            string                 abilityName   = entry != null ? entry.abilityName             : string.Empty;
            string                 specialtyName = entry != null ? entry.designatedSpecialtyName : string.Empty;

            item.SetupWithSelection(
                allAbilities, specialtiesByArea,
                OnItemSelectionChanged, OpenWindowForItem,
                abilityName, specialtyName);
        }

        LockDefaultAbilitySelections();
        RefreshAllItems();

        isApplyingSaveData = false;
        SyncAbilityDataToManager();
        ApplyAbilityStatEffects();
        ApplyAbilitySpecialEffects();
    }

    public void ResetAbilitySelections()
    {
        ApplyAbilitySaveData(null);
    }

    // ─── 기본 어빌리티 잠금 ──────────────────────────────────

    private void LockDefaultAbilitySelections()
    {
        LockAbilitySelection(0);
        LockAbilitySelection(1);
    }

    private void LockAbilitySelection(int index)
    {
        if (index < 0 || index >= abilityItems.Count)
            return;

        AbilityItem item = abilityItems[index];
        if (item != null)
            item.SetAbilitySelectionLocked(true);
    }

    // ─── 저장 항목 구성 ──────────────────────────────────────

    private List<InsaneAbilityEntryData> BuildApplyEntries(InsaneAbilityData saveData)
    {
        List<InsaneAbilityEntryData> entries = new List<InsaneAbilityEntryData>();

        if (saveData != null && saveData.abilities != null && saveData.abilities.Count > 0)
        {
            for (int i = 0; i < saveData.abilities.Count; i++)
            {
                InsaneAbilityEntryData entry = saveData.abilities[i];
                entries.Add(new InsaneAbilityEntryData
                {
                    abilityName             = entry != null ? entry.abilityName             : string.Empty,
                    designatedSpecialtyName = entry != null ? entry.designatedSpecialtyName : string.Empty
                });
            }
        }
        else if (saveData != null && saveData.acquiredAbilityNames != null)
        {
            for (int i = 0; i < saveData.acquiredAbilityNames.Count; i++)
            {
                entries.Add(new InsaneAbilityEntryData
                {
                    abilityName             = saveData.acquiredAbilityNames[i],
                    designatedSpecialtyName = string.Empty
                });
            }
        }

        EnsureEntry(entries, 0).abilityName = DefaultFirstAbilityName;
        EnsureEntry(entries, 1).abilityName = DefaultSecondAbilityName;
        return entries;
    }

    private InsaneAbilityEntryData EnsureEntry(List<InsaneAbilityEntryData> entries, int index)
    {
        while (entries.Count <= index)
            entries.Add(new InsaneAbilityEntryData());

        if (entries[index] == null)
            entries[index] = new InsaneAbilityEntryData();

        return entries[index];
    }

    // ─── 스탯 이펙트 ─────────────────────────────────────────

    private void ApplyAbilityStatEffects()
    {
        EnsureInsaneManager();

        if (insaneManager == null)
            return;

        int totalLife      = 0;
        int totalSanity    = 0;
        int totalSpecialty = 0;

        for (int i = 0; i < abilityItems.Count; i++)
        {
            AbilityStatEffect effect = abilityItems[i]?.SelectedAbilityData?.statEffect;
            if (effect == null || effect.IsEmpty)
                continue;

            totalLife      += effect.lifeBonus;
            totalSanity    += effect.sanityBonus;
            totalSpecialty += effect.specialtyBonus;
        }

        insaneManager.SetAbilityStatBonus(totalLife, totalSanity, totalSpecialty);
    }

    /// <summary>
    /// 현재 선택된 어빌리티의 특수 이벤트를 집계해 InsaneManager에 전달합니다.
    /// 어빌리티 선택/해제 때마다 ApplyAbilityStatEffects()와 함께 호출됩니다.
    /// </summary>
    private void ApplyAbilitySpecialEffects()
    {
        EnsureInsaneManager();

        if (insaneManager == null)
            return;

        bool wrapHorizontally = false;

        for (int i = 0; i < abilityItems.Count; i++)
        {
            AbilitySpecialEffect effect = abilityItems[i]?.SelectedAbilityData?.specialEffect
                                          ?? AbilitySpecialEffect.None;

            if (effect == AbilitySpecialEffect.WrapHorizontally)
                wrapHorizontally = true;
        }

        insaneManager.SetAbilitySpecialEffects(wrapHorizontally);
    }

    // ─── Manager 동기화 ───────────────────────────────────────

    private void SyncAbilityDataToManager()
    {
        EnsureInsaneManager();

        if (insaneManager != null)
            insaneManager.SetAbilitySaveData(CreateAbilitySaveData());
    }

    private void EnsureInsaneManager()
    {
        if (insaneManager == null)
            insaneManager = FindObjectOfType<InsaneManager>();
    }
}
