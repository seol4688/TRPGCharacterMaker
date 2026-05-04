using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InsaneManager : MonoBehaviour
{
    private Profile profile;
    private Specialty specialty;
    private Ability ability;

    [Header("DefaultData")]
    public const int DefaultLife = 6;
    public const int DefaultSanity = 6;
    public const int DefaultMeritPoint = 0;
    public const int DefaultCheckedSpecialty = 6;
    public const int DefaultFearSpecialtyCount = 1;
    public const int DefaultItemCount = 2;
    public const int DefaultAbilityCount = 2;
    private const int WeirdAreaSanityPenalty = 1;

    //[Header("Avatar")]
    //[SerializeField] private RawImage profileRawImage;
    //private AspectRatioFitter profileAspectRatioFitter;
    //private static readonly AspectRatioFitter.AspectMode ProfileImageAspectMode = AspectRatioFitter.AspectMode.FitInParent;

    [Header("Stat")]
    private int maxLife = DefaultLife;
    private int maxSanity = DefaultSanity;
    private int meritPoint = DefaultMeritPoint;
    private int abilityLifeBonus      = 0; // 어빌리티 스탯 이펙트 합산값
    private int abilitySanityBonus    = 0;
    private int abilitySpecialtyBonus = 0;

    [Header("Specialty")]
    [SerializeField] private SpecialtyNameDatabase specialtyNameDatabase;
    private int maxCheckedSpecialty = DefaultCheckedSpecialty;
    private int maxFearSpecialtyCount = DefaultFearSpecialtyCount;
    private string weirdAreaName = string.Empty;

    [Header("Ability")]
    [SerializeField] private AbilityDatabase officialAbilityDatabase;
    [SerializeField] private AbilityDatabase unofficialAbilityDatabase;
    [SerializeField] private AbilityDatabase ishikiAbilityDatabase;
    private int maxAbilityCount = DefaultAbilityCount;

    [Header("경고")]
    [SerializeField] private TextMeshProUGUI itemCountText;
    [SerializeField] private TextMeshProUGUI specialtyCountText;
    [SerializeField] private TextMeshProUGUI curiosityCountText;
    [SerializeField] private TextMeshProUGUI fearCountText;
    [SerializeField] private TextMeshProUGUI abilityCountText;

    [Header("Growth")]
    [Tooltip("성장 창 컴포넌트. 캐릭터 로드 시 공적점·공포심 UI를 자동 갱신합니다.")]
    [SerializeField] private GrowthWindow    growthWindow;
    [Tooltip("공적점 사용 창 컴포넌트. 캐릭터 로드 시 UI를 자동 갱신합니다.")]
    [SerializeField] private UsePointWindow  usePointWindow;

    [Header("Modal")]
    [Tooltip("인세인 전용 불러오기 모달")]
    [SerializeField] private LoadModalController_Insane loadModal;

    [Space(50f)]
    [SerializeField] private InsaneCharacterSheet currentSheet;


    public InsaneCharacterSheet CurrentSheet       => currentSheet;
    public LoadModalController_Insane LoadModal    => loadModal;

    public int MaxLife               => maxLife;
    public int MaxSanity             => maxSanity;
    public int MeritPoint            => meritPoint;
    public int MaxCheckedSpecialty   => maxCheckedSpecialty;
    public int MaxFearSpecialtyCount => maxFearSpecialtyCount;
    public int MaxAbilityCount       => maxAbilityCount;

    private void Awake()
    {
        if(profile == null)
        {
            profile = FindObjectOfType<Profile>();
            profile.SetInsaneManager(this);
        }
        else
        {
            profile.SetInsaneManager(this);
        }

        if (specialty == null)
        {
            specialty = FindObjectOfType<Specialty>();
            //profile.SetInsaneManager(this);
        }
        else
        {
            //profile.SetInsaneManager(this);
        }

        if (ability == null)
        {
            ability = FindObjectOfType<Ability>();
            ability.SetInsaneManager(this);
        }
        else
        {
            ability.SetInsaneManager(this);
        }

        EnsureAbilityReference();
        EnsureCurrentSheet();
    }

    private void OnEnable()
    {
        if (specialty != null)
        {
            specialty.CheckedSpecialtyChanged += HandleCheckedSpecialtyChanged;
            specialty.FearSpecialtyChanged    += HandleFearSpecialtyChanged;
            specialty.SkillNamesChanged       += HandleSkillNamesChanged;
            specialty.CuriosityAreaChanged    += HandleCuriosityAreaChanged;
        }

        ResetCharacterSheet();
    }

    private void OnDisable()
    {
        if (specialty != null)
        {
            specialty.CheckedSpecialtyChanged -= HandleCheckedSpecialtyChanged;
            specialty.FearSpecialtyChanged    -= HandleFearSpecialtyChanged;
            specialty.SkillNamesChanged       -= HandleSkillNamesChanged;
            specialty.CuriosityAreaChanged    -= HandleCuriosityAreaChanged;
        }
    }

    private void OnDestroy()
    {
        profile.ClearLoadedProfileTexture();
    }

    private void OnValidate()
    {
        maxLife = Mathf.Max(1, maxLife);
        maxSanity = Mathf.Max(0, maxSanity);
        EnsureCurrentSheet();
        UpdateSanityFromSpecialtySelection();
    }

    public void ResetCharacterSheet()
    {
        InsaneCharacterSheet freshSheet = new InsaneCharacterSheet();
        ApplyRuleDefaults(freshSheet, true);
        ApplyCharacterSheetSaveData(freshSheet);
    }

    /// <summary>
    /// Setting_Insane의 General 탭 적용 버튼에서 호출합니다.
    /// 기본 수치를 모두 갱신하고 연관 UI를 재계산합니다.
    /// </summary>
    public void ApplyGeneralSettings(int life, int sanity, int merit, int specialty, int fear, int ability)
    {
        maxLife               = Mathf.Max(1, life);
        maxSanity             = Mathf.Max(0, sanity);
        meritPoint            = Mathf.Max(0, merit);
        maxCheckedSpecialty   = Mathf.Max(1, specialty);
        maxFearSpecialtyCount = Mathf.Max(1, fear);
        maxAbilityCount       = Mathf.Max(0, ability);

        UpdateSanityFromSpecialtySelection(); // 생명력·이성치·공적점 표시 갱신
        ApplyCheckedSpecialtyMax();           // 특성 최대치 → CheckedSpecialtyChanged 이벤트 → UpdateRemainingCountText
        ApplyFearSpecialtyMax();              // 공포심 최대치 → FearSpecialtyChanged 이벤트 → UpdateFearCountText
        this.ability?.TrimToAbilityMax();     // 새 최대치 초과 어빌리티를 뒤에서부터 선택 해제
        UpdateAbilityCountText();             // 어빌리티 남은 수 갱신
    }

    //public void OpenProfileImageFile()
    //{
    //    FileBrowserManager.OpenImageFile(ApplyProfileImage);
    //}

    public void OnClick_CharacterSave()
    {
        OpenCharacterSaveWindow();
    }

    public void OnClick_CharacterLoad()
    {
        OpenCharacterLoadWindow();
    }

    public void OpenCharacterSaveWindow()
    {
        DataManager dataManager = Manager.Data;
        if (dataManager == null)
        {
            return;
        }

        if (Manager.SaveModal != null)
        {
            Manager.SaveModal.Open("캐릭터 저장", string.Empty, SaveCharacterSheet);
            return;
        }

        SaveCharacterSheet(string.Empty);
    }

    public void OpenCharacterLoadWindow()
    {
        DataManager dataManager = Manager.Data;
        if (dataManager == null)
        {
            return;
        }

        LoadModalController modal = loadModal != null ? loadModal : Manager.LoadModal;
        if (modal != null)
        {
            string[] saveNames = dataManager.GetInsaneCharacterSheetSaveNames();
            LoadModalController.CharacterLoadEntry[] entries = new LoadModalController.CharacterLoadEntry[saveNames.Length];
            for (int i = 0; i < saveNames.Length; i++)
            {
                InsaneCharacterSheet sheet = dataManager.LoadInsaneCharacterSheet(saveNames[i]);
                entries[i] = new LoadModalController.CharacterLoadEntry
                {
                    saveName        = saveNames[i],
                    characterName   = sheet?.profile?.name,
                    ruleName        = "인세인",
                    avatarImagePath = sheet?.profile?.avatarImagePath,
                    creationDate    = dataManager.GetInsaneCharacterSheetCreationTime(saveNames[i])
                };
            }
            modal.OpenCharacter("캐릭터 불러오기", string.Empty, entries, LoadCharacterSheet, DeleteCharacterSheet);
            return;
        }

        LoadCharacterSheet(string.Empty);
    }

    private void SaveCharacterSheet(string saveName)
    {
        DataManager dataManager = Manager.Data;
        if (dataManager == null)
        {
            return;
        }

        dataManager.SaveInsaneCharacterSheet(this, saveName);
    }

    private void LoadCharacterSheet(string saveName)
    {
        DataManager dataManager = Manager.Data;
        if (dataManager == null)
        {
            return;
        }

        dataManager.LoadInsaneCharacterSheet(this, saveName);
    }

    private void DeleteCharacterSheet(string saveName)
    {
        DataManager dataManager = Manager.Data;
        if (dataManager == null)
        {
            return;
        }

        dataManager.DeleteInsaneCharacterSheet(saveName);
    }

    public bool AddStartingItem(InsaneItemType itemType)
    {
        EnsureCurrentSheet();

        string itemName = GetItemName(itemType);
        if (currentSheet.item.startingItemNames.Count >= GetTotalItemCount())
        {
            return false;
        }

        currentSheet.item.startingItemNames.Add(itemName);
        AddItemCount(itemName, 1);
        UpdateItemCountText();
        return true;
    }

    public void SetStartingItemSlot(int slotIndex, int itemTypeIndex)
    {
        if (slotIndex < 0 || slotIndex >= DefaultItemCount || !Enum.IsDefined(typeof(InsaneItemType), itemTypeIndex))
        {
            return;
        }

        SetStartingItemSlot(slotIndex, (InsaneItemType)itemTypeIndex);
    }

    public void SetStartingItemSlot(int slotIndex, InsaneItemType itemType)
    {
        EnsureCurrentSheet();

        if (slotIndex < 0 || slotIndex >= DefaultItemCount)
        {
            return;
        }

        string itemName = GetItemName(itemType);
        if (slotIndex < currentSheet.item.startingItemNames.Count)
        {
            string previousItemName = currentSheet.item.startingItemNames[slotIndex];
            currentSheet.item.startingItemNames[slotIndex] = itemName;
            RemoveItemCount(previousItemName, 1);
            AddItemCount(itemName, 1);
            return;
        }

        while (currentSheet.item.startingItemNames.Count < slotIndex)
        {
            currentSheet.item.startingItemNames.Add(itemName);
            AddItemCount(itemName, 1);
        }

        if (currentSheet.item.startingItemNames.Count < DefaultItemCount)
        {
            currentSheet.item.startingItemNames.Add(itemName);
            AddItemCount(itemName, 1);
        }
    }

    public void RemoveStartingItemAt(int slotIndex)
    {
        EnsureCurrentSheet();

        if (slotIndex < 0 || slotIndex >= currentSheet.item.startingItemNames.Count)
        {
            return;
        }

        string removedItemName = currentSheet.item.startingItemNames[slotIndex];
        currentSheet.item.startingItemNames.RemoveAt(slotIndex);
        RemoveItemCount(removedItemName, 1);
        UpdateItemCountText();
    }

    public void RefreshSpecialtyDropdowns()
    {
        if (specialty == null)
        {
            return;
        }

        EnsureCurrentSheet();

        string[] areaNames = specialtyNameDatabase != null ? specialtyNameDatabase.GetAreaNames() : null;
        specialty.SetDefaultSkillNames(GetDefaultSkillNamesFromDatabase());
        specialty.ApplyAreaNames(areaNames);
        specialty.Initialize();
        specialty.SetCheckedSpecialtyMax(maxCheckedSpecialty + abilitySpecialtyBonus);

        RefreshWeirdAreaNameFromSpecialty();
        specialty.RefreshCuriosityDropdown(currentSheet.specialty.curiosityAreaName);
    }

    public string[] GetDefaultAreaNames()
    {
        return specialtyNameDatabase != null ? specialtyNameDatabase.GetAreaNames() : new string[SpecialtyNameDatabase.ColumnCount];
    }

    public List<AbilityData> GetAllAbilities()
    {
        List<AbilityData> result = new List<AbilityData>();
        AppendAbilitiesFromDatabase(result, officialAbilityDatabase);
        AppendAbilitiesFromDatabase(result, unofficialAbilityDatabase);
        AppendAbilitiesFromDatabase(result, ishikiAbilityDatabase);
        return result;
    }

    public List<AbilityData> GetOfficialAbilities()   => BuildAbilityListFromDatabase(officialAbilityDatabase);
    public List<AbilityData> GetUnofficialAbilities() => BuildAbilityListFromDatabase(unofficialAbilityDatabase);
    public List<AbilityData> GetIshikiAbilities()     => BuildAbilityListFromDatabase(ishikiAbilityDatabase);

    private List<AbilityData> BuildAbilityListFromDatabase(AbilityDatabase database)
    {
        List<AbilityData> result = new List<AbilityData>();
        AppendAbilitiesFromDatabase(result, database);
        return result;
    }

    public Dictionary<string, List<string>> GetSpecialtiesByArea()
    {
        return specialty != null
            ? specialty.GetSpecialtyNamesByArea()
            : new Dictionary<string, List<string>>();
    }

    private void AppendAbilitiesFromDatabase(List<AbilityData> result, AbilityDatabase database)
    {
        if (database == null)
        {
            return;
        }

        IReadOnlyList<AbilityData> abilities = database.Abilities;
        for (int i = 0; i < abilities.Count; i++)
        {
            if (abilities[i] != null)
            {
                result.Add(abilities[i]);
            }
        }
    }

    public string[] GetDefaultSkillNames()
    {
        return GetDefaultSkillNamesFromDatabase();
    }

    private string[] GetDefaultSkillNamesFromDatabase()
    {
        int count = SpecialtyNameDatabase.SkillItemCount;
        string[] names = new string[count];
        for (int i = 0; i < count; i++)
            names[i] = specialtyNameDatabase != null ? specialtyNameDatabase.GetSkillName(i) : string.Empty;
        return names;
    }

    /// <summary>현재 특성 그리드에 표시 중인 영역 이름 배열을 반환합니다.</summary>
    public string[] GetCurrentAreaNames()
        => specialty != null ? specialty.GetCurrentAreaNames() : GetDefaultAreaNames();

    /// <summary>
    /// 현재 적용된 커스텀 스킬 이름 배열을 반환합니다.
    /// 커스텀 이름이 없으면 null을 반환합니다.
    /// </summary>
    public string[] GetCurrentCustomSkillNames()
        => specialty?.GetCurrentCustomSkillNames();

    public void ApplyCustomNames(string[] areaNames, string[] skillNames)
    {
        if (specialty == null)
        {
            return;
        }

        specialty.ApplyCustomSheetSaveData(new CustomSheetSaveData
        {
            areaNames = areaNames,
            skillNames = skillNames
        });

        specialty.Initialize();
        RefreshWeirdAreaNameFromSpecialty();
        specialty.RefreshCuriosityDropdown(currentSheet.specialty.curiosityAreaName);
    }

    private void RefreshWeirdAreaNameFromSpecialty()
    {
        if (specialty == null)
        {
            weirdAreaName = string.Empty;
            return;
        }

        List<string> areaNames = specialty.GetCuriosityAreaNames();
        weirdAreaName = areaNames.Count > 0 ? areaNames[areaNames.Count - 1] : string.Empty;
    }


    public InsaneCharacterSheet CreateCharacterSheetSaveData()
    {
        EnsureCurrentSheet();
        SyncProfileFromInputFields();
        InsaneCharacterSheet saveData = CloneCharacterSheet(currentSheet);
        if (specialty != null)
        {
            saveData.specialty = specialty.CreateSpecialtySaveData();
        }

        if (ability != null)
        {
            saveData.ability = ability.CreateAbilitySaveData();
        }

        return saveData;
    }

    public void ApplyCharacterSheetSaveData(InsaneCharacterSheet saveData)
    {
        EnsureAbilityReference();
        currentSheet = saveData != null ? CloneCharacterSheet(saveData) : new InsaneCharacterSheet();
        NormalizeCharacterSheet(currentSheet);

        if (specialty != null)
        {
            // ApplySpecialtySaveData를 먼저 호출해 specialty.FearSpecialtyNames를 saveData 기준으로 초기화한 뒤,
            // ApplyFearSpecialtyMax()에서 FearSpecialtyChanged 이벤트가 발생할 때 HandleFearSpecialtyChanged()가
            // 올바른(초기화된) 공포심 데이터를 읽도록 순서를 보장합니다.
            RefreshSpecialtyDropdowns();
            specialty.ApplySpecialtySaveData(currentSheet.specialty);
            RefreshWeirdAreaNameFromSpecialty();
            specialty.RefreshCuriosityDropdown(currentSheet.specialty.curiosityAreaName);
            ApplyFearSpecialtyMax();
        }

        UpdateSanityFromSpecialtySelection();
        UpdateRemainingCountText();
        UpdateFearCountText();
        UpdateCuriosityCountText();
        UpdateItemCountText();
        RefreshProfileUI();

        if (ability != null)
        {
            ability.SetInsaneManager(this);
            ability.ApplyAbilitySaveData(currentSheet.ability);
        }

        growthWindow?.RefreshUI();
        usePointWindow?.RefreshUI();
    }

    public void SetAbilitySaveData(InsaneAbilityData abilityData)
    {
        EnsureCurrentSheet();
        currentSheet.ability = CloneAbilityData(abilityData);
        NormalizeAbilityData(currentSheet.ability);
        UpdateAbilityCountText();
    }

    private void RefreshProfileUI()
    {
        EnsureCurrentSheet();
        profile?.RefreshProfileUI(currentSheet);
    }

    private void SyncProfileFromInputFields()
    {
        EnsureCurrentSheet();
        profile?.SyncProfileFromInputFields(currentSheet);
    }

    //private void LoadProfileImageFromPath(string imagePath)
    //{
    //    if (profileRawImage == null)
    //    {
    //        return;
    //    }

    //    FileBrowserManager.ImageLoadResult imageLoadResult = FileBrowserManager.LoadImageTextureOrDefault(imagePath);
    //    currentSheet.profile.avatarImagePath = imageLoadResult.loadedFromPath ? imagePath : string.Empty;
    //    SetProfileRawImageTexture(imageLoadResult.texture, imageLoadResult.shouldDestroyTexture);
    //}

    //private void ApplyProfileImage(string imagePath, Texture2D texture)
    //{
    //    if (texture == null)
    //    {
    //        return;
    //    }

    //    EnsureCurrentSheet();
    //    currentSheet.profile.avatarImagePath = imagePath ?? string.Empty;
    //    SetProfileRawImageTexture(texture, true);
    //}

    //private void SetProfileRawImageTexture(Texture2D texture, bool shouldDestroyWhenReplaced)
    //{
    //    Texture2D previousTexture = loadedProfileTexture;
    //    bool shouldDestroyPreviousTexture = loadedProfileTextureShouldDestroy;

    //    loadedProfileTexture = texture;
    //    loadedProfileTextureShouldDestroy = shouldDestroyWhenReplaced;

    //    if (profileRawImage != null)
    //    {
    //        FileBrowserManager.ApplyTextureToRawImage(
    //            profileRawImage,
    //            texture,
    //            ref profileAspectRatioFitter,
    //            true,
    //            ProfileImageAspectMode);
    //    }

    //    FileBrowserManager.DestroyTextureIfNeeded(previousTexture, shouldDestroyPreviousTexture && previousTexture != texture);
    //}

    //private void ClearLoadedProfileTexture()
    //{
    //    if (loadedProfileTexture == null)
    //    {
    //        return;
    //    }

    //    if (profileRawImage != null && profileRawImage.texture == loadedProfileTexture)
    //    {
    //        profileRawImage.texture = null;
    //    }

    //    FileBrowserManager.DestroyTextureIfNeeded(loadedProfileTexture, loadedProfileTextureShouldDestroy);
    //    loadedProfileTexture = null;
    //    loadedProfileTextureShouldDestroy = false;
    //}

    private void HandleCuriosityAreaChanged(string areaName)
    {
        EnsureCurrentSheet();
        currentSheet.specialty.curiosityAreaName = areaName;
        UpdateCuriosityCountText();
    }

    private void HandleSkillNamesChanged()
    {
        // 특성 이름이 바뀌면 AbilityItem 드롭다운을 갱신합니다.
        EnsureAbilityReference();
        ability?.RefreshAllItems();
    }

    private void HandleCheckedSpecialtyChanged()
    {
        UpdateSanityFromSpecialtySelection();
        UpdateRemainingCountText();
    }

    private void HandleFearSpecialtyChanged()
    {
        EnsureCurrentSheet();
        currentSheet.specialty.fearSpecialtyNames = new List<string>(specialty.FearSpecialtyNames);
        UpdateFearCountText();
    }

    private void UpdateRemainingCountText()
    {
        if (specialtyCountText == null || specialty == null)
            return;

        int remaining = (maxCheckedSpecialty + abilitySpecialtyBonus) - specialty.GetCheckedSpecialtyCount();

        if (remaining <= 0)
        {
            specialtyCountText.gameObject.SetActive(false);
            return;
        }

        specialtyCountText.gameObject.SetActive(true);
        specialtyCountText.color = Color.red;
        specialtyCountText.text  = $"남은 특성 {remaining}개";
    }

    private void UpdateFearCountText()
    {
        if (fearCountText == null || specialty == null)
            return;

        int remaining = GetTotalFearCount() - specialty.FearSpecialtyNames.Count;

        if (remaining <= 0)
        {
            fearCountText.gameObject.SetActive(false);
            return;
        }

        fearCountText.gameObject.SetActive(true);
        fearCountText.color = Color.red;
        fearCountText.text  = $"남은 공포심 {remaining}개";
    }

    private void UpdateCuriosityCountText()
    {
        if (curiosityCountText == null)
            return;

        bool hasSelection = !string.IsNullOrEmpty(currentSheet?.specialty?.curiosityAreaName);
        curiosityCountText.gameObject.SetActive(!hasSelection);
    }

    private void UpdateAbilityCountText()
    {
        if (abilityCountText == null)
            return;

        int selected = 0;
        List<InsaneAbilityEntryData> abilities = currentSheet?.ability?.abilities;
        if (abilities != null)
        {
            // index 0·1은 잠긴 기본 어빌리티 — index 2부터 선택 슬롯
            for (int i = 2; i < abilities.Count; i++)
            {
                if (!string.IsNullOrEmpty(abilities[i]?.abilityName))
                    selected++;
            }
        }

        int remaining = GetTotalAbilityCount() - selected;
        if (remaining <= 0)
        {
            abilityCountText.gameObject.SetActive(false);
            return;
        }

        abilityCountText.gameObject.SetActive(true);
        abilityCountText.color = Color.red;
        abilityCountText.text  = $"남은 어빌리티 {remaining}개";
    }

    private void UpdateItemCountText()
    {
        if (itemCountText == null) return;

        int total    = GetTotalItemCount();
        int selected = currentSheet?.item?.startingItemNames?.Count ?? 0;
        int remaining = total - selected;

        if (remaining <= 0)
        {
            itemCountText.gameObject.SetActive(false);
            return;
        }

        itemCountText.gameObject.SetActive(true);
        itemCountText.color = Color.red;
        itemCountText.text  = $"남은 아이템 {remaining}개";
    }

    private void UpdateSanityFromSpecialtySelection()
    {
        EnsureCurrentSheet();

        int weirdSpecialtyCount = specialty != null ? specialty.GetCheckedSpecialtyCountInArea(weirdAreaName) : 0;
        int sanityPenalty       = weirdSpecialtyCount * WeirdAreaSanityPenalty;

        currentSheet.maxLife   = maxLife + abilityLifeBonus;
        currentSheet.maxSanity = Mathf.Max(0, maxSanity - sanityPenalty + abilitySanityBonus);
        UpdateStatTexts();
    }

    /// <summary>
    /// Ability.cs에서 호출. 선택된 어빌리티의 스탯 보너스 합산값을 받아 스탯·특성수를 재계산합니다.
    /// </summary>
    public void SetAbilityStatBonus(int lifeBonus, int sanityBonus, int specialtyBonus)
    {
        abilityLifeBonus      = lifeBonus;
        abilitySanityBonus    = sanityBonus;
        abilitySpecialtyBonus = specialtyBonus;
        UpdateSanityFromSpecialtySelection();
        ApplyCheckedSpecialtyMax();
    }

    private void ApplyCheckedSpecialtyMax()
    {
        if (specialty != null)
            specialty.SetCheckedSpecialtyMax(maxCheckedSpecialty + abilitySpecialtyBonus);
    }

    /// <summary>
    /// Setting의 기본 공포심 수와 Growth의 공포심 증감분을 합산하여 Specialty에 전달합니다.
    /// </summary>
    private void ApplyFearSpecialtyMax()
    {
        if (specialty != null)
            specialty.SetFearSpecialtyMax(Mathf.Max(1, GetTotalFearCount()));
    }

    /// <summary>Setting 기본값 + Growth 증감분(fearLevel - DefaultFearLevel).</summary>
    private int GetTotalFearCount()
    {
        int growthDelta = (currentSheet?.growth?.fearLevel ?? InsaneGrowthData.DefaultFearLevel)
                          - InsaneGrowthData.DefaultFearLevel;
        return maxFearSpecialtyCount + growthDelta;
    }

    /// <summary>
    /// Ability.cs에서 호출. 현재 선택된 어빌리티의 특수 이벤트 합산 결과를 적용합니다.
    /// </summary>
    public void SetAbilitySpecialEffects(bool wrapHorizontally)
    {
        if (specialty != null)
            specialty.SetWrapHorizontally(wrapHorizontally);
    }

    // ─── 성장 데이터 접근자 ───────────────────────────────────

    // 공적점 사용 비용 상수
    public const int AbilityAcquisitionCost = 4;
    public const int FearOvercomeCost = 5;
    public const int EconomyRiseCost  = 2;

    /// <summary>설정 기본값 + growth 보너스를 포함한 실질 어빌리티 선택 슬롯 수.</summary>
    public int GetTotalAbilityCount()
        => maxAbilityCount + (currentSheet?.growth?.abilityBonus ?? 0);

    /// <summary>기본 아이템 수 + growth 보너스를 포함한 실질 아이템 슬롯 수.</summary>
    public int GetTotalItemCount()
        => DefaultItemCount + (currentSheet?.growth?.itemBonus ?? 0);

    /// <summary>어빌리티 습득에 필요한 공적점 (현재 슬롯 수 × 4).</summary>
    public int GetAbilityAcquisitionCost()
        => GetTotalAbilityCount() * AbilityAcquisitionCost;

    /// <summary>
    /// 어빌리티 습득을 수행합니다.
    /// 공적점이 충분하면 비용을 차감하고 슬롯 +1. 부족하면 false 반환.
    /// </summary>
    public bool TryAcquireAbilitySlot()
    {
        EnsureCurrentSheet();
        int cost = GetAbilityAcquisitionCost();
        if (GetMeritPoints() < cost) return false;

        currentSheet.growth.abilityBonus++;
        AddMeritPoints(-cost);       // UpdateStatTexts 포함
        UpdateAbilityCountText();
        return true;
    }

    /// <summary>
    /// 공포심 극복을 수행합니다.
    /// 공적점 5 차감 후 growth.fearLevel -1 (최솟값 1). 조건 미충족 시 false 반환.
    /// </summary>
    public bool TryOvercomeFear()
    {
        EnsureCurrentSheet();
        if (GetMeritPoints() < FearOvercomeCost)              return false;
        if (GetFearLevel() <= InsaneGrowthData.MinFearLevel)  return false;

        ChangeFearLevel(-1);         // ApplyFearSpecialtyMax + UpdateFearCountText 포함
        AddMeritPoints(-FearOvercomeCost);
        return true;
    }

    /// <summary>
    /// 경제력 상승을 수행합니다.
    /// 공적점 5 차감 후 아이템 슬롯 +1. 공적점 부족 시 false 반환.
    /// </summary>
    public bool TryRiseEconomy()
    {
        EnsureCurrentSheet();
        if (GetMeritPoints() < EconomyRiseCost) return false;

        currentSheet.growth.itemBonus++;
        AddMeritPoints(-EconomyRiseCost);
        UpdateItemCountText();
        return true;
    }

    /// <summary>설정 기본값 + 누적 growth 공적점의 합산값을 반환합니다.</summary>
    public int GetMeritPoints()
    {
        EnsureCurrentSheet();
        return meritPoint + currentSheet.growth.meritPoints;
    }

    /// <summary>공적점을 추가합니다 (음수도 허용).</summary>
    public void AddMeritPoints(int amount)
    {
        EnsureCurrentSheet();
        currentSheet.growth.meritPoints += amount;
        UpdateStatTexts();
    }

    /// <summary>현재 공포심을 반환합니다.</summary>
    public int GetFearLevel()
    {
        EnsureCurrentSheet();
        return currentSheet.growth.fearLevel;
    }

    /// <summary>
    /// 공포심을 delta만큼 변경합니다.
    /// 결과값은 InsaneGrowthData.MinFearLevel 이상으로 클램프됩니다.
    /// </summary>
    public void ChangeFearLevel(int delta)
    {
        EnsureCurrentSheet();
        currentSheet.growth.fearLevel = Mathf.Max(
            InsaneGrowthData.MinFearLevel,
            currentSheet.growth.fearLevel + delta);

        ApplyFearSpecialtyMax();
        UpdateFearCountText();
    }

    private void UpdateStatTexts()
    {
        profile?.UpdateStatDisplay(currentSheet.maxLife, currentSheet.maxSanity, GetMeritPoints());
    }

    public void EnsureCurrentSheet()
    {
        if (currentSheet == null)
        {
            currentSheet = new InsaneCharacterSheet();
            ApplyRuleDefaults(currentSheet, true);
            return;
        }

        NormalizeCharacterSheet(currentSheet);
    }

    private void EnsureAbilityReference()
    {
        if (ability == null)
        {
            ability = FindObjectOfType<Ability>();
        }
    }

    private void ApplyRuleDefaults(InsaneCharacterSheet sheet, bool resetCurrentValues)
    {
        NormalizeCharacterSheet(sheet);

        sheet.maxLife = maxLife;
        sheet.maxSanity = maxSanity;

        if (resetCurrentValues)
        {
            sheet.item.startingItemNames.Clear();
            sheet.item.items.Clear();
            sheet.ability.acquiredAbilityNames.Clear();
            sheet.ability.abilities.Clear();
            EnsureDefaultAbilityData(sheet.ability);
            sheet.growth.meritPoints  = 0;
            sheet.growth.fearLevel    = InsaneGrowthData.DefaultFearLevel;
            sheet.growth.abilityBonus = 0;
            sheet.growth.itemBonus    = 0;
            return;
        }
    }

    private void NormalizeCharacterSheet(InsaneCharacterSheet sheet)
    {
        if (sheet == null)
        {
            return;
        }

        if (sheet.profile == null)
        {
            sheet.profile = new InsaneProfileData();
        }

        if (sheet.item == null)
        {
            sheet.item = new InsaneItemData();
        }

        if (sheet.item.startingItemNames == null)
        {
            sheet.item.startingItemNames = new List<string>();
        }

        if (sheet.item.items == null)
        {
            sheet.item.items = new List<InsaneItemStackData>();
        }

        if (sheet.specialty == null)
        {
            sheet.specialty = new InsaneSpecialtyData();
        }

        if (sheet.specialty.specialties == null)
        {
            sheet.specialty.specialties = new List<InsaneSpecialtyEntryData>();
        }

        if (sheet.specialty.fearSpecialtyNames == null)
        {
            sheet.specialty.fearSpecialtyNames = new List<string>();
        }

        if (sheet.ability == null)
        {
            sheet.ability = new InsaneAbilityData();
        }

        NormalizeAbilityData(sheet.ability);

        if (sheet.growth == null)
        {
            sheet.growth = new InsaneGrowthData();
        }

        if (sheet.growth.fearLevel < InsaneGrowthData.MinFearLevel)
        {
            sheet.growth.fearLevel = InsaneGrowthData.MinFearLevel;
        }

        if (sheet.maxLife <= 0)
        {
            sheet.maxLife = maxLife;
        }

        if (sheet.maxSanity < 0)
        {
            sheet.maxSanity = maxSanity;
        }

        for (int i = sheet.item.startingItemNames.Count - 1; i >= 0; i--)
        {
            if (i >= DefaultItemCount + sheet.growth.itemBonus)
            {
                sheet.item.startingItemNames.RemoveAt(i);
            }
        }

        for (int i = sheet.item.items.Count - 1; i >= 0; i--)
        {
            InsaneItemStackData itemStack = sheet.item.items[i];
            if (itemStack == null || string.IsNullOrWhiteSpace(itemStack.itemName) || itemStack.count <= 0)
            {
                sheet.item.items.RemoveAt(i);
            }
        }
    }


    private void NormalizeAbilityData(InsaneAbilityData abilityData)
    {
        if (abilityData == null)
        {
            return;
        }

        if (abilityData.acquiredAbilityNames == null)
        {
            abilityData.acquiredAbilityNames = new List<string>();
        }

        if (abilityData.abilities == null)
        {
            abilityData.abilities = new List<InsaneAbilityEntryData>();
        }

        if (abilityData.abilities.Count == 0 && abilityData.acquiredAbilityNames.Count > 0)
        {
            for (int i = 0; i < abilityData.acquiredAbilityNames.Count; i++)
            {
                abilityData.abilities.Add(new InsaneAbilityEntryData
                {
                    abilityName = abilityData.acquiredAbilityNames[i],
                    designatedSpecialtyName = string.Empty
                });
            }
        }

        EnsureDefaultAbilityData(abilityData);
    }

    private void EnsureDefaultAbilityData(InsaneAbilityData abilityData)
    {
        if (abilityData == null)
        {
            return;
        }

        if (abilityData.acquiredAbilityNames == null)
        {
            abilityData.acquiredAbilityNames = new List<string>();
        }

        if (abilityData.abilities == null)
        {
            abilityData.abilities = new List<InsaneAbilityEntryData>();
        }

        SetDefaultAbilityEntry(abilityData, 0, Ability.DefaultFirstAbilityName);
        SetDefaultAbilityEntry(abilityData, 1, Ability.DefaultSecondAbilityName);
        RebuildAcquiredAbilityNames(abilityData);
    }

    private void SetDefaultAbilityEntry(InsaneAbilityData abilityData, int index, string abilityName)
    {
        while (abilityData.abilities.Count <= index)
        {
            abilityData.abilities.Add(new InsaneAbilityEntryData());
        }

        if (abilityData.abilities[index] == null)
        {
            abilityData.abilities[index] = new InsaneAbilityEntryData();
        }

        abilityData.abilities[index].abilityName = abilityName;
    }

    private void RebuildAcquiredAbilityNames(InsaneAbilityData abilityData)
    {
        abilityData.acquiredAbilityNames.Clear();

        for (int i = 0; i < abilityData.abilities.Count; i++)
        {
            InsaneAbilityEntryData entry = abilityData.abilities[i];
            if (entry != null && !string.IsNullOrWhiteSpace(entry.abilityName))
            {
                abilityData.acquiredAbilityNames.Add(entry.abilityName);
            }
        }
    }

    private string GetItemName(InsaneItemType itemType)
    {
        switch (itemType)
        {
            case InsaneItemType.Painkiller:
                return "\uC9C4\uD1B5\uC81C";
            case InsaneItemType.Weapon:
                return "\uBB34\uAE30";
            case InsaneItemType.Talisman:
                return "\uBD80\uC801";
            default:
                return itemType.ToString();
        }
    }

    private void AddItemCount(string itemName, int count)
    {
        if (string.IsNullOrWhiteSpace(itemName) || count <= 0)
        {
            return;
        }

        for (int i = 0; i < currentSheet.item.items.Count; i++)
        {
            if (currentSheet.item.items[i].itemName == itemName)
            {
                currentSheet.item.items[i].count += count;
                return;
            }
        }

        currentSheet.item.items.Add(new InsaneItemStackData
        {
            itemName = itemName,
            count = count
        });
    }

    private InsaneCharacterSheet CloneCharacterSheet(InsaneCharacterSheet sheet)
    {
        NormalizeCharacterSheet(sheet);

        InsaneCharacterSheet clone = new InsaneCharacterSheet
        {
            profile = new InsaneProfileData
            {
                avatarImagePath = sheet.profile.avatarImagePath,
                name = sheet.profile.name,
                gender = sheet.profile.gender,
                age = sheet.profile.age,
                job = sheet.profile.job
            },
            maxLife   = sheet.maxLife,
            maxSanity = sheet.maxSanity,
            item      = CloneItemData(sheet.item),
            specialty = CloneSpecialtyData(sheet.specialty),
            ability   = CloneAbilityData(sheet.ability),
            growth    = new InsaneGrowthData
            {
                meritPoints  = sheet.growth.meritPoints,
                fearLevel    = sheet.growth.fearLevel,
                abilityBonus = sheet.growth.abilityBonus,
                itemBonus    = sheet.growth.itemBonus
            }
        };

        NormalizeCharacterSheet(clone);
        return clone;
    }

    private InsaneAbilityData CloneAbilityData(InsaneAbilityData abilityData)
    {
        InsaneAbilityData clone = new InsaneAbilityData
        {
            acquiredAbilityNames = new List<string>(),
            abilities = new List<InsaneAbilityEntryData>()
        };

        if (abilityData == null)
        {
            return clone;
        }

        if (abilityData.acquiredAbilityNames != null)
        {
            clone.acquiredAbilityNames = new List<string>(abilityData.acquiredAbilityNames);
        }

        if (abilityData.abilities != null)
        {
            for (int i = 0; i < abilityData.abilities.Count; i++)
            {
                InsaneAbilityEntryData entry = abilityData.abilities[i];
                clone.abilities.Add(new InsaneAbilityEntryData
                {
                    abilityName = entry != null ? entry.abilityName : string.Empty,
                    designatedSpecialtyName = entry != null ? entry.designatedSpecialtyName : string.Empty
                });
            }
        }

        return clone;
    }

    private InsaneItemData CloneItemData(InsaneItemData itemData)
    {
        InsaneItemData clone = new InsaneItemData
        {
            startingItemNames = new List<string>(itemData.startingItemNames),
            items = new List<InsaneItemStackData>()
        };

        for (int i = 0; i < itemData.items.Count; i++)
        {
            clone.items.Add(new InsaneItemStackData
            {
                itemName = itemData.items[i].itemName,
                count = itemData.items[i].count
            });
        }

        return clone;
    }

    private InsaneSpecialtyData CloneSpecialtyData(InsaneSpecialtyData specialtyData)
    {
        InsaneSpecialtyData clone = new InsaneSpecialtyData
        {
            areaNames = specialtyData.areaNames != null ? (string[])specialtyData.areaNames.Clone() : null,
            curiosityAreaName = specialtyData.curiosityAreaName,
            fearSpecialtyNames = new List<string>(specialtyData.fearSpecialtyNames ?? new List<string>()),
            specialties = new List<InsaneSpecialtyEntryData>()
        };

        for (int i = 0; i < specialtyData.specialties.Count; i++)
        {
            InsaneSpecialtyEntryData specialtyEntry = specialtyData.specialties[i];
            clone.specialties.Add(new InsaneSpecialtyEntryData
            {
                column = specialtyEntry.column,
                row = specialtyEntry.row,
                specialtyName = specialtyEntry.specialtyName,
                difficulty = specialtyEntry.difficulty,
                isChecked = specialtyEntry.isChecked
            });
        }

        return clone;
    }

    private void RemoveItemCount(string itemName, int count)
    {
        if (string.IsNullOrWhiteSpace(itemName) || count <= 0)
        {
            return;
        }

        for (int i = 0; i < currentSheet.item.items.Count; i++)
        {
            InsaneItemStackData itemStack = currentSheet.item.items[i];
            if (itemStack.itemName == itemName)
            {
                itemStack.count -= count;
                if (itemStack.count <= 0)
                {
                    currentSheet.item.items.RemoveAt(i);
                }

                return;
            }
        }
    }
}
