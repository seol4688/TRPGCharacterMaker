using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AbilityItem : MonoBehaviour
{
    private const string EmptyOption            = "-";
    private const string RequiredSpecialtyOption = "선택필요";

    [Header("이름 / 제거")]
    [SerializeField] private Button          nameButton;
    [Tooltip("nameButton 안의 텍스트")]
    [SerializeField] private TextMeshProUGUI nameButtonText;
    //[SerializeField] private Button          removeButton;

    [Header("자동 입력")]
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private TMP_Dropdown    specialtyDropdown;
    [SerializeField] private Color           requiredSpecialtyTextColor = Color.red;
    [SerializeField] private TextMeshProUGUI effectText;

    private List<AbilityData>                availableAbilities = new List<AbilityData>();
    private Dictionary<string, List<string>> specialtiesByArea  = new Dictionary<string, List<string>>();
    private AbilityData                      selectedAbility;
    private Action<AbilityItem>              onSelectionChanged;
    private Action<AbilityItem>              onOpenWindowRequested;
    private bool                             isPopulating             = false;
    private bool                             isAbilitySelectionLocked = false;
    private Color                            defaultSpecialtyTextColor    = Color.white;
    private bool                             hasDefaultSpecialtyTextColor = false;

    public string      SelectedAbilityName   => selectedAbility?.name ?? string.Empty;
    public string      SelectedSpecialtyName => BuildSelectedSpecialtyName();
    public AbilityData SelectedAbilityData   => selectedAbility;

    // ─── 이벤트 등록 ─────────────────────────────────────────

    private void Awake()
    {
        CacheSpecialtyTextColor();
    }

    private void OnEnable()
    {
        if (nameButton != null)
            nameButton.onClick.AddListener(OnNameButtonClicked);

        //if (removeButton != null)
        //    removeButton.onClick.AddListener(OnRemoveButtonClicked);

        if (specialtyDropdown != null)
            specialtyDropdown.onValueChanged.AddListener(OnSpecialtyDropdownChanged);

        CacheSpecialtyTextColor();
        RefreshSpecialtyRequirementState();
    }

    private void OnDisable()
    {
        if (nameButton != null)
            nameButton.onClick.RemoveListener(OnNameButtonClicked);

        //if (removeButton != null)
        //    removeButton.onClick.RemoveListener(OnRemoveButtonClicked);

        if (specialtyDropdown != null)
            specialtyDropdown.onValueChanged.RemoveListener(OnSpecialtyDropdownChanged);
    }

    // ─── 초기화 ──────────────────────────────────────────────

    /// <summary>
    /// 필터링된 어빌리티 목록을 받아 초기화합니다.
    /// 이전 선택이 available 목록에 없으면 선택이 해제됩니다.
    /// </summary>
    public void Setup(
        List<AbilityData>                abilities,
        Dictionary<string, List<string>> areaSpecialties,
        Action<AbilityItem>              selectionChangedCallback,
        Action<AbilityItem>              openWindowCallback = null)
    {
        onSelectionChanged    = selectionChangedCallback;
        onOpenWindowRequested = openWindowCallback;
        availableAbilities    = abilities       ?? new List<AbilityData>();
        specialtiesByArea     = areaSpecialties ?? new Dictionary<string, List<string>>();

        string previousSpecialtyName = SelectedSpecialtyName;

        ValidateSelection();
        RefreshNameButtonText();
        RefreshAutoFields();
        TrySelectSpecialty(previousSpecialtyName);
        RefreshNameButtonInteractable();
    }

    /// <summary>
    /// 저장 데이터 복원 시 호출. 전체 어빌리티 목록에서 이름으로 찾아 선택합니다.
    /// </summary>
    public void SetupWithSelection(
        List<AbilityData>                abilities,
        Dictionary<string, List<string>> areaSpecialties,
        Action<AbilityItem>              selectionChangedCallback,
        Action<AbilityItem>              openWindowCallback,
        string                           abilityName,
        string                           specialtyName)
    {
        onSelectionChanged    = selectionChangedCallback;
        onOpenWindowRequested = openWindowCallback;
        availableAbilities    = abilities       ?? new List<AbilityData>();
        specialtiesByArea     = areaSpecialties ?? new Dictionary<string, List<string>>();

        RestoreSelection(abilityName);

        RefreshNameButtonText();
        RefreshAutoFields();
        TrySelectSpecialty(specialtyName);
        RefreshNameButtonInteractable();
    }

    /// <summary>AbilitySelectWindow에서 어빌리티를 선택했을 때 Ability.cs가 호출합니다.</summary>
    public void SelectAbility(AbilityData data)
    {
        selectedAbility = data;
        RefreshNameButtonText();
        RefreshAutoFields();
        onSelectionChanged?.Invoke(this);
    }

    public bool TrySelectAbility(string abilityName)
    {
        if (string.IsNullOrWhiteSpace(abilityName))
            return false;

        for (int i = 0; i < availableAbilities.Count; i++)
        {
            if (availableAbilities[i]?.name == abilityName)
            {
                selectedAbility = availableAbilities[i];
                RefreshNameButtonText();
                RefreshAutoFields();
                return true;
            }
        }

        return false;
    }

    public bool TrySelectSpecialty(string specialtyName)
    {
        if (specialtyDropdown == null || string.IsNullOrWhiteSpace(specialtyName))
            return false;

        if (specialtyDropdown.options == null)
            return false;

        for (int i = 0; i < specialtyDropdown.options.Count; i++)
        {
            if (specialtyDropdown.options[i].text == specialtyName)
            {
                specialtyDropdown.SetValueWithoutNotify(i);
                specialtyDropdown.RefreshShownValue();
                RefreshSpecialtyRequirementState();
                return true;
            }
        }

        return false;
    }

    public void ResetSelection()
    {
        selectedAbility = null;
        RefreshNameButtonText();
        RefreshAutoFields();
    }

    public void SetAbilitySelectionLocked(bool isLocked)
    {
        isAbilitySelectionLocked = isLocked;
        RefreshNameButtonInteractable();
        //RefreshRemoveButtonInteractable();
    }

    // ─── 이름 버튼 ────────────────────────────────────────────

    private void OnNameButtonClicked()
    {
        if (isAbilitySelectionLocked)
            return;

        onOpenWindowRequested?.Invoke(this);
    }

    //private void OnRemoveButtonClicked()
    //{
    //    if (isAbilitySelectionLocked)
    //        return;

    //    selectedAbility = null;
    //    RefreshNameButtonText();
    //    RefreshAutoFields();
    //    onSelectionChanged?.Invoke(this);
    //}

    private void RefreshNameButtonText()
    {
        if (nameButtonText == null)
            return;

        nameButtonText.text = string.IsNullOrEmpty(SelectedAbilityName)
            ? EmptyOption
            : SelectedAbilityName;
    }

    private void RefreshNameButtonInteractable()
    {
        if (nameButton == null)
            return;

        nameButton.interactable = !isAbilitySelectionLocked;
    }

    //private void RefreshRemoveButtonInteractable()
    //{
    //    if (removeButton == null)
    //        return;

    //    removeButton.interactable = !isAbilitySelectionLocked && selectedAbility != null;
    //}

    // ─── 선택 복원 / 검증 ────────────────────────────────────

    /// <summary>availableAbilities에 없으면 선택 해제합니다.</summary>
    private void ValidateSelection()
    {
        if (selectedAbility == null)
            return;

        string currentName = selectedAbility.name;
        for (int i = 0; i < availableAbilities.Count; i++)
        {
            if (availableAbilities[i]?.name == currentName)
                return;
        }

        selectedAbility = null;
    }

    /// <summary>availableAbilities에서 이름으로 찾아 selectedAbility를 설정합니다.</summary>
    private void RestoreSelection(string abilityName)
    {
        selectedAbility = null;

        if (string.IsNullOrEmpty(abilityName))
            return;

        for (int i = 0; i < availableAbilities.Count; i++)
        {
            if (availableAbilities[i]?.name == abilityName)
            {
                selectedAbility = availableAbilities[i];
                return;
            }
        }
    }

    // ─── 자동 입력 ────────────────────────────────────────────

    private void RefreshAutoFields()
    {
        RefreshTypeText();
        RefreshSpecialtyDropdown();
        RefreshEffectText();
        //RefreshRemoveButtonInteractable();
    }

    private void RefreshTypeText()
    {
        if (typeText == null)
            return;

        typeText.text = selectedAbility != null
            ? GetTypeDisplayText(selectedAbility.type)
            : string.Empty;
    }

    private void RefreshEffectText()
    {
        if (effectText == null)
            return;

        effectText.text = selectedAbility?.effect ?? string.Empty;
    }

    // ─── 지정특기 드롭다운 ────────────────────────────────────

    private void RefreshSpecialtyDropdown()
    {
        if (specialtyDropdown == null)
            return;

        isPopulating = true;

        List<string> options           = BuildSpecialtyOptions();
        bool         requiresSelection = RequiresSpecialtySelection(selectedAbility) && options.Count > 1;

        if (requiresSelection)
            options.Insert(0, RequiredSpecialtyOption);

        specialtyDropdown.ClearOptions();

        if (options.Count == 0)
        {
            specialtyDropdown.AddOptions(new List<string> { string.Empty });
            specialtyDropdown.SetValueWithoutNotify(0);
            specialtyDropdown.interactable = false;
            isPopulating = false;
            RefreshSpecialtyRequirementState();
            return;
        }

        specialtyDropdown.AddOptions(options);
        specialtyDropdown.SetValueWithoutNotify(0);

        bool autoFilled = !requiresSelection
            || options.Count == 1
            || selectedAbility == null
            || selectedAbility.designatedSpecialtyType == DesignatedSpecialtyType.None
            || selectedAbility.designatedSpecialtyType == DesignatedSpecialtyType.Variable;

        specialtyDropdown.interactable = !autoFilled;

        isPopulating = false;
        RefreshSpecialtyRequirementState();
    }

    private void OnSpecialtyDropdownChanged(int index)
    {
        if (isPopulating)
            return;

        RefreshSpecialtyRequirementState();
        onSelectionChanged?.Invoke(this);
    }

    private List<string> BuildSpecialtyOptions()
    {
        if (selectedAbility == null)
            return new List<string>();

        switch (selectedAbility.designatedSpecialtyType)
        {
            case DesignatedSpecialtyType.None:
                return new List<string> { "없음" };

            case DesignatedSpecialtyType.Variable:
                return new List<string> { "가변" };

            case DesignatedSpecialtyType.AnySpecialty:
                return BuildAllSpecialtyOptions();

            default:
                return BuildEntryOptions(selectedAbility.designatedEntries);
        }
    }

    /// <summary>분야 구분 없이 specialtiesByArea 전체에서 중복 없이 특성 목록을 빌드합니다.</summary>
    private List<string> BuildAllSpecialtyOptions()
    {
        List<string> options = new List<string>();

        foreach (List<string> areaSpecialties in specialtiesByArea.Values)
        {
            for (int i = 0; i < areaSpecialties.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(areaSpecialties[i]) && !options.Contains(areaSpecialties[i]))
                    options.Add(areaSpecialties[i]);
            }
        }

        return options;
    }

    private List<string> BuildEntryOptions(List<DesignatedSpecialtyEntry> entries)
    {
        List<string> options = new List<string>();

        if (entries == null)
            return options;

        for (int i = 0; i < entries.Count; i++)
        {
            DesignatedSpecialtyEntry entry = entries[i];

            if (entry.isAreaEntry)
            {
                if (specialtiesByArea.TryGetValue(entry.value, out List<string> areaSpecialties))
                {
                    for (int j = 0; j < areaSpecialties.Count; j++)
                    {
                        if (!options.Contains(areaSpecialties[j]))
                            options.Add(areaSpecialties[j]);
                    }
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(entry.value) && !options.Contains(entry.value))
                    options.Add(entry.value);
            }
        }

        return options;
    }

    // ─── 데이터 읽기 ─────────────────────────────────────────

    private string BuildSelectedSpecialtyName()
    {
        if (selectedAbility == null || specialtyDropdown == null)
            return string.Empty;

        if (selectedAbility.designatedSpecialtyType == DesignatedSpecialtyType.None)
            return "없음";

        if (selectedAbility.designatedSpecialtyType == DesignatedSpecialtyType.Variable)
            return "가변";

        if (specialtyDropdown.options == null || specialtyDropdown.options.Count == 0)
            return string.Empty;

        string selectedText = specialtyDropdown.options[specialtyDropdown.value].text;
        return selectedText == RequiredSpecialtyOption ? string.Empty : selectedText;
    }

    private bool RequiresSpecialtySelection(AbilityData ability)
    {
        if (ability == null)
            return false;

        switch (ability.designatedSpecialtyType)
        {
            case DesignatedSpecialtyType.Specific:
            case DesignatedSpecialtyType.AnyInArea:
            case DesignatedSpecialtyType.Mixed:
            case DesignatedSpecialtyType.AnySpecialty:
                return true;
            default:
                return false;
        }
    }

    private bool IsRequiredSpecialtyPlaceholderSelected()
    {
        if (selectedAbility == null || specialtyDropdown == null)
            return false;

        if (!RequiresSpecialtySelection(selectedAbility))
            return false;

        if (specialtyDropdown.options == null || specialtyDropdown.options.Count == 0)
            return false;

        int value = specialtyDropdown.value;
        return value >= 0
            && value < specialtyDropdown.options.Count
            && specialtyDropdown.options[value].text == RequiredSpecialtyOption;
    }

    private void RefreshSpecialtyRequirementState()
    {
        if (specialtyDropdown == null || specialtyDropdown.captionText == null)
            return;

        CacheSpecialtyTextColor();

        specialtyDropdown.captionText.color = IsRequiredSpecialtyPlaceholderSelected()
            ? requiredSpecialtyTextColor
            : defaultSpecialtyTextColor;
    }

    private void CacheSpecialtyTextColor()
    {
        if (hasDefaultSpecialtyTextColor || specialtyDropdown == null || specialtyDropdown.captionText == null)
            return;

        defaultSpecialtyTextColor    = specialtyDropdown.captionText.color;
        hasDefaultSpecialtyTextColor = true;
    }

    private string GetTypeDisplayText(InsaneAbilityType type)
    {
        switch (type)
        {
            case InsaneAbilityType.Attack:    return "공격";
            case InsaneAbilityType.Support:   return "서포트";
            case InsaneAbilityType.Equipment: return "장비";
            default:                          return string.Empty;
        }
    }
}
