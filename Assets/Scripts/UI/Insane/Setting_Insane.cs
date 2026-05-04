using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Setting_Insane : Setting
{
    [SerializeField] private InsaneManager insaneManager;

    [System.Serializable]
    private struct StatControl
    {
        public TextMeshProUGUI valueText;
        public Button          plusButton;
        public Button          minusButton;
    }

    // ════════════════════════════════════════════════════════════
    #region General

    [Header("General")]
    [SerializeField] private StatControl life;
    [SerializeField] private StatControl sanity;
    [SerializeField] private StatControl merit;
    [SerializeField] private StatControl specialty;
    [SerializeField] private StatControl fear;
    [SerializeField] private StatControl ability;
    [SerializeField] private Button      resetButton_General;
    [SerializeField] private Button      applyButton_General;

    private int pendingLife;
    private int pendingMaxSanity;
    private int pendingMeritPoint;
    private int pendingMaxCheckedSpecialty;
    private int pendingMaxFearSpecialtyCount;
    private int pendingMaxAbilityCount;

    private void LoadGeneralPendingValues()
    {
        if (insaneManager == null) return;

        pendingLife                  = insaneManager.MaxLife;
        pendingMaxSanity             = insaneManager.MaxSanity;
        pendingMeritPoint            = insaneManager.MeritPoint;
        pendingMaxCheckedSpecialty   = insaneManager.MaxCheckedSpecialty;
        pendingMaxFearSpecialtyCount = insaneManager.MaxFearSpecialtyCount;
        pendingMaxAbilityCount       = insaneManager.MaxAbilityCount;
    }

    private void RefreshGeneralUI()
    {
        SetText(life.valueText,      pendingLife);
        SetText(sanity.valueText,    pendingMaxSanity);
        SetText(merit.valueText,     pendingMeritPoint);
        SetText(specialty.valueText, pendingMaxCheckedSpecialty);
        SetText(fear.valueText,      pendingMaxFearSpecialtyCount);
        SetText(ability.valueText,   pendingMaxAbilityCount);
    }

    private void OnLifePlusClicked()       { pendingLife++;                                                    RefreshGeneralUI(); }
    private void OnLifeMinusClicked()      { pendingLife               = Mathf.Max(1, pendingLife - 1);        RefreshGeneralUI(); }
    private void OnSanityPlusClicked()     { pendingMaxSanity++;                                               RefreshGeneralUI(); }
    private void OnSanityMinusClicked()    { pendingMaxSanity          = Mathf.Max(0, pendingMaxSanity - 1);   RefreshGeneralUI(); }
    private void OnMeritPlusClicked()      { pendingMeritPoint++;                                              RefreshGeneralUI(); }
    private void OnMeritMinusClicked()     { pendingMeritPoint         = Mathf.Max(0, pendingMeritPoint - 1);  RefreshGeneralUI(); }
    private void OnSpecialtyPlusClicked()  { pendingMaxCheckedSpecialty++;                                     RefreshGeneralUI(); }
    private void OnSpecialtyMinusClicked() { pendingMaxCheckedSpecialty = Mathf.Max(1, pendingMaxCheckedSpecialty - 1); RefreshGeneralUI(); }
    private void OnFearPlusClicked()       { pendingMaxFearSpecialtyCount++;                                   RefreshGeneralUI(); }
    private void OnFearMinusClicked()      { pendingMaxFearSpecialtyCount = Mathf.Max(1, pendingMaxFearSpecialtyCount - 1); RefreshGeneralUI(); }
    private void OnAbilityPlusClicked()    { pendingMaxAbilityCount++;                                        RefreshGeneralUI(); }
    private void OnAbilityMinusClicked()   { pendingMaxAbilityCount    = Mathf.Max(0, pendingMaxAbilityCount - 1); RefreshGeneralUI(); }

    private void OnApplyGeneralButtonClicked()
    {
        ApplyModalController modal = Manager.ApplyModal;
        if (modal != null)
            modal.Open("일반 설정 적용", "설정한 수치를 적용하시겠습니까?", OnApplyGeneralClicked);
        else
            OnApplyGeneralClicked();
    }

    private void OnApplyGeneralClicked()
    {
        if (insaneManager == null) return;

        insaneManager.ApplyGeneralSettings(
            pendingLife,
            pendingMaxSanity,
            pendingMeritPoint,
            pendingMaxCheckedSpecialty,
            pendingMaxFearSpecialtyCount,
            pendingMaxAbilityCount);
    }

    private void OnResetGeneralClicked()
    {
        pendingLife                  = InsaneManager.DefaultLife;
        pendingMaxSanity             = InsaneManager.DefaultSanity;
        pendingMeritPoint            = InsaneManager.DefaultMeritPoint;
        pendingMaxCheckedSpecialty   = InsaneManager.DefaultCheckedSpecialty;
        pendingMaxFearSpecialtyCount = InsaneManager.DefaultFearSpecialtyCount;
        pendingMaxAbilityCount       = InsaneManager.DefaultAbilityCount;
        RefreshGeneralUI();
    }

    #endregion
    // ════════════════════════════════════════════════════════════
    #region CustomSpecialty

    [Header("CustomSpecialty")]
    [SerializeField] private TMP_InputField[] areaInputFields  = new TMP_InputField[SpecialtyNameDatabase.ColumnCount];
    [SerializeField] private TMP_InputField[] skillInputFields = new TMP_InputField[SpecialtyNameDatabase.SkillItemCount];
    [SerializeField] private Button           saveButton;
    [SerializeField] private Button           loadButton;
    [SerializeField] private Button           applyButton;

    private void RefreshPlaceholders()
    {
        if (insaneManager == null) return;

        ApplyPlaceholders(areaInputFields,  insaneManager.GetDefaultAreaNames());
        ApplyPlaceholders(skillInputFields, insaneManager.GetDefaultSkillNames());
    }

    /// <summary>
    /// 현재 적용된 커스텀 이름이 기본값과 다를 경우 input field에 채워 넣습니다.
    /// </summary>
    private void FillCurrentValues()
    {
        if (insaneManager == null) return;

        string[] currentAreaNames = insaneManager.GetCurrentAreaNames();
        string[] defaultAreaNames = insaneManager.GetDefaultAreaNames();
        FillIfDifferent(areaInputFields, currentAreaNames, defaultAreaNames);

        string[] customSkillNames = insaneManager.GetCurrentCustomSkillNames();
        FillInputFields(skillInputFields, customSkillNames);
    }

    private void OnApplyCustomSheetButtonClicked()
    {
        ApplyModalController modal = Manager.ApplyModal;
        if (modal != null)
            modal.Open("커스텀 시트 적용", "특성 이름을 변경하시겠습니까?", OnapplyClicked);
        else
            OnapplyClicked();
    }

    private void OnapplyClicked()
    {
        if (insaneManager == null) return;
        insaneManager.ApplyCustomNames(CollectValues(areaInputFields), CollectValues(skillInputFields));
    }

    private void OnSaveClicked()
    {
        DataManager dataManager = Manager.Data;
        if (dataManager == null) return;

        if (Manager.SaveModal != null)
        {
            Manager.SaveModal.Open("커스텀 시트 저장", string.Empty, SaveCustomSheet);
            return;
        }

        SaveCustomSheet(string.Empty);
    }

    private void SaveCustomSheet(string saveName)
    {
        DataManager dataManager = Manager.Data;
        if (dataManager == null) return;

        CustomSheetSaveData saveData = new CustomSheetSaveData
        {
            areaNames  = CollectValues(areaInputFields),
            skillNames = CollectValues(skillInputFields)
        };

        dataManager.SaveInsaneCustomSheet(saveData, saveName);
    }

    private void OnLoadClicked()
    {
        DataManager dataManager = Manager.Data;
        if (dataManager == null) return;

        LoadModalController_Insane modal = insaneManager?.LoadModal;
        if (modal != null)
        {
            string[] saveNames = dataManager.GetInsaneCustomSheetSaveNames();
            modal.Open("커스텀 시트 불러오기", string.Empty, saveNames, LoadCustomSheet, DeleteCustomSheet);
            return;
        }

        LoadCustomSheet(string.Empty);
    }

    private void DeleteCustomSheet(string saveName)
    {
        Manager.Data?.DeleteInsaneCustomSheet(saveName);
    }

    private void LoadCustomSheet(string saveName)
    {
        DataManager dataManager = Manager.Data;
        if (dataManager == null) return;

        CustomSheetSaveData saveData = dataManager.LoadInsaneCustomSheet(saveName);
        if (saveData == null) return;

        FillInputFields(areaInputFields,  saveData.areaNames);
        FillInputFields(skillInputFields, saveData.skillNames);
    }

    #endregion
    // ════════════════════════════════════════════════════════════
    #region Lifecycle

    private void OnEnable()
    {
        // General
        LoadGeneralPendingValues();
        RefreshGeneralUI();
        AddListener(life.plusButton,      OnLifePlusClicked);
        AddListener(life.minusButton,     OnLifeMinusClicked);
        AddListener(sanity.plusButton,    OnSanityPlusClicked);
        AddListener(sanity.minusButton,   OnSanityMinusClicked);
        AddListener(merit.plusButton,     OnMeritPlusClicked);
        AddListener(merit.minusButton,    OnMeritMinusClicked);
        AddListener(specialty.plusButton, OnSpecialtyPlusClicked);
        AddListener(specialty.minusButton,OnSpecialtyMinusClicked);
        AddListener(fear.plusButton,      OnFearPlusClicked);
        AddListener(fear.minusButton,     OnFearMinusClicked);
        AddListener(ability.plusButton,   OnAbilityPlusClicked);
        AddListener(ability.minusButton,  OnAbilityMinusClicked);
        AddListener(applyButton_General,  OnApplyGeneralButtonClicked);
        AddListener(resetButton_General,  OnResetGeneralClicked);

        // CustomSpecialty
        RefreshPlaceholders();
        FillCurrentValues();
        AddListener(applyButton, OnApplyCustomSheetButtonClicked);
        AddListener(saveButton,  OnSaveClicked);
        AddListener(loadButton,  OnLoadClicked);
    }

    private void OnDisable()
    {
        // General
        RemoveListener(life.plusButton,      OnLifePlusClicked);
        RemoveListener(life.minusButton,     OnLifeMinusClicked);
        RemoveListener(sanity.plusButton,    OnSanityPlusClicked);
        RemoveListener(sanity.minusButton,   OnSanityMinusClicked);
        RemoveListener(merit.plusButton,     OnMeritPlusClicked);
        RemoveListener(merit.minusButton,    OnMeritMinusClicked);
        RemoveListener(specialty.plusButton, OnSpecialtyPlusClicked);
        RemoveListener(specialty.minusButton,OnSpecialtyMinusClicked);
        RemoveListener(fear.plusButton,      OnFearPlusClicked);
        RemoveListener(fear.minusButton,     OnFearMinusClicked);
        RemoveListener(ability.plusButton,   OnAbilityPlusClicked);
        RemoveListener(ability.minusButton,  OnAbilityMinusClicked);
        RemoveListener(applyButton_General,  OnApplyGeneralButtonClicked);
        RemoveListener(resetButton_General,  OnResetGeneralClicked);

        // CustomSpecialty
        RemoveListener(applyButton, OnApplyCustomSheetButtonClicked);
        RemoveListener(saveButton,  OnSaveClicked);
        RemoveListener(loadButton,  OnLoadClicked);
    }

    #endregion
    // ════════════════════════════════════════════════════════════
    #region Helpers

    private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null) button.onClick.AddListener(action);
    }

    private static void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null) button.onClick.RemoveListener(action);
    }

    private static void SetText(TextMeshProUGUI text, int value)
    {
        if (text != null) text.text = value.ToString();
    }

    private static void FillIfDifferent(
        TMP_InputField[] inputFields,
        string[]         current,
        string[]         defaults)
    {
        if (inputFields == null || current == null) return;

        for (int i = 0; i < inputFields.Length && i < current.Length; i++)
        {
            if (inputFields[i] == null) continue;

            string cur = current[i] ?? string.Empty;
            string def = (defaults != null && i < defaults.Length) ? defaults[i] ?? string.Empty : string.Empty;

            if (cur != def)
                inputFields[i].SetTextWithoutNotify(cur);
        }
    }

    private void ApplyPlaceholders(TMP_InputField[] inputFields, string[] names)
    {
        if (inputFields == null || names == null) return;

        for (int i = 0; i < inputFields.Length && i < names.Length; i++)
        {
            if (inputFields[i] == null) continue;

            TMP_Text placeholder = inputFields[i].placeholder as TMP_Text;
            if (placeholder != null)
                placeholder.text = names[i] ?? string.Empty;

            inputFields[i].SetTextWithoutNotify(string.Empty);
        }
    }

    private void FillInputFields(TMP_InputField[] inputFields, string[] values)
    {
        if (inputFields == null || values == null) return;

        for (int i = 0; i < inputFields.Length && i < values.Length; i++)
        {
            if (inputFields[i] == null) continue;
            inputFields[i].SetTextWithoutNotify(values[i] ?? string.Empty);
        }
    }

    private string[] CollectValues(TMP_InputField[] inputFields)
    {
        if (inputFields == null) return null;

        string[] values = new string[inputFields.Length];

        for (int i = 0; i < inputFields.Length; i++)
        {
            if (inputFields[i] == null)
            {
                values[i] = string.Empty;
                continue;
            }

            if (!string.IsNullOrWhiteSpace(inputFields[i].text))
            {
                values[i] = inputFields[i].text;
            }
            else
            {
                TMP_Text placeholder = inputFields[i].placeholder as TMP_Text;
                values[i] = placeholder != null ? placeholder.text : string.Empty;
            }
        }

        return values;
    }

    #endregion
}
