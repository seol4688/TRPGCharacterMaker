using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Specialty : MonoBehaviour
{
    private class SpecialtyAreaInfo
    {
        public string areaName;
        public Transform transform;
        public float xPosition;
    }

    private const int ColumnCount = 6;
    private const int RowCount = 11;
    private const int SpecialtyItemCount = ColumnCount * RowCount;
    private const int HorizontalStepCost = 2;
    private const int CuriosityHorizontalStepCost = 1;
    private const int VerticalStepCost = 1;
    [SerializeField] private SpecialtyItem[] specialtyItems = new SpecialtyItem[SpecialtyItemCount];
    private string[] defaultSkillNames = new string[SpecialtyItemCount];
    private List<SpecialtyAreaInfo> cachedAreas;
    [SerializeField] private Image[] bars;
    [SerializeField, Min(6)] private int checkedSpecialty = InsaneManager.DefaultCheckedSpecialty;
    [SerializeField, Min(1)] private int checkedFear = InsaneManager.DefaultFearSpecialtyCount;
    [SerializeField] private bool wrapHorizontally;
    [SerializeField] private Color normalBarColor = Color.white;
    [SerializeField] private Color curiosityBarColor = Color.yellow;
    [SerializeField] private string curiosityAreaName;
    //[SerializeField, Min(1)] private int maxFearSpecialtyCount = InsaneManager.DefaultFearSpecialtyCount;
    [SerializeField] private List<string> fearSpecialtyNames = new List<string>();
    [SerializeField] private bool useCustomSkillNames;
    [SerializeField] private string[] customSkillNames = new string[SpecialtyItemCount];

    [Header("Traits")]
    [SerializeField] private TMP_Dropdown curiosityDropdown;
    private const string NoneDropdownOptionText = "-";
    [SerializeField] private TextMeshProUGUI fearSpecialtyText;


    public string CuriosityAreaName => curiosityAreaName;
    public IReadOnlyList<string> FearSpecialtyNames => fearSpecialtyNames;
    public event Action CheckedSpecialtyChanged;
    public event Action FearSpecialtyChanged;
    public event Action SkillNamesChanged;
    public event Action<string> CuriosityAreaChanged;

    private void OnEnable()
    {
        if (curiosityDropdown != null)
            curiosityDropdown.onValueChanged.AddListener(HandleCuriosityDropdownChanged);
    }

    private void OnDisable()
    {
        if (curiosityDropdown != null)
            curiosityDropdown.onValueChanged.RemoveListener(HandleCuriosityDropdownChanged);
    }

    private void Awake()
    {
        Initialize();
    }

    private void OnValidate()
    {
        checkedSpecialty = Mathf.Max(0, checkedSpecialty);

        if (specialtyItems == null || specialtyItems.Length != SpecialtyItemCount)
        {
            specialtyItems = new SpecialtyItem[SpecialtyItemCount];
        }

        Initialize();
    }

    public void Initialize()
    {
        cachedAreas = null;
        TryAutoAssignFromChildren();
        EnsureSkillNameStorage();
        ApplySkillNames();
        RegisterItems();
        ApplySkillStates();
        ApplyCuriosityBars();
        RecalculateCounts();
    }

    [ContextMenu("Auto Fill From Children")]
    private void TryAutoAssignFromChildren()
    {
        bool hasEmptySlot = false;
        for (int i = 0; i < specialtyItems.Length; i++)
        {
            if (specialtyItems[i] == null)
            {
                hasEmptySlot = true;
                break;
            }
        }

        if (!hasEmptySlot)
        {
            return;
        }

        SpecialtyItem[] childItems = GetComponentsInChildren<SpecialtyItem>(true);
        if (childItems.Length != SpecialtyItemCount)
        {
            return;
        }

        for (int i = 0; i < childItems.Length; i++)
        {
            specialtyItems[i] = childItems[i];
        }
    }

    public List<string> GetCuriosityAreaNames()
    {
        List<SpecialtyAreaInfo> areas = GetCuriosityAreasInVisualOrder();
        List<string> areaNames = new List<string>();
        for (int i = 0; i < areas.Count; i++)
        {
            areaNames.Add(areas[i].areaName);
        }

        return areaNames;
    }

    public Dictionary<string, List<string>> GetSpecialtyNamesByArea()
    {
        List<SpecialtyAreaInfo> areas = GetCuriosityAreasInVisualOrder();
        Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();

        for (int col = 0; col < areas.Count; col++)
        {
            string areaName = areas[col].areaName;
            List<string> names = new List<string>();

            for (int row = 0; row < RowCount; row++)
            {
                int index = GetFlatIndex(col, row);
                if (index >= 0)
                {
                    string skillName = GetSkillNameForIndex(index);
                    if (!string.IsNullOrWhiteSpace(skillName))
                    {
                        names.Add(skillName);
                    }
                }
            }

            result[areaName] = names;
        }

        return result;
    }

    private List<SpecialtyAreaInfo> GetCuriosityAreasInVisualOrder()
    {
        if (cachedAreas != null)
        {
            return cachedAreas;
        }

        List<SpecialtyAreaInfo> areas = new List<SpecialtyAreaInfo>();
        Transform[] children = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            if (!children[i].name.StartsWith("SpecialtyArea"))
            {
                continue;
            }

            TextMeshProUGUI areaNameText = FindAreaNameText(children[i]);
            if (areaNameText != null && !string.IsNullOrWhiteSpace(areaNameText.text))
            {
                areas.Add(new SpecialtyAreaInfo
                {
                    areaName = areaNameText.text,
                    transform = children[i],
                    xPosition = GetAreaCenterX(children[i])
                });
            }
        }

        areas.Sort(CompareAreaPosition);
        cachedAreas = areas;
        return cachedAreas;
    }

    private int CompareAreaPosition(SpecialtyAreaInfo left, SpecialtyAreaInfo right)
    {
        int positionCompare = left.xPosition.CompareTo(right.xPosition);
        if (positionCompare != 0)
        {
            return positionCompare;
        }

        return left.transform.GetSiblingIndex().CompareTo(right.transform.GetSiblingIndex());
    }

    private float GetAreaCenterX(Transform areaTransform)
    {
        RectTransform rectTransform = areaTransform as RectTransform;
        if (rectTransform == null)
        {
            return areaTransform.position.x;
        }

        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        return (corners[0].x + corners[2].x) * 0.5f;
    }

    // 특정 영역에 체크된 특성이 1개 이상 있는지 반환하는 bool 래퍼.
    // GetCheckedSpecialtyCountInArea()를 직접 쓰는 쪽(InsaneManager)으로 통일되어 현재 미사용.
    // 단순 존재 여부 조건 분기가 필요해지면 주석 해제하여 사용.
    // public bool HasCheckedSpecialtyInArea(string areaName)
    // {
    //     return GetCheckedSpecialtyCountInArea(areaName) > 0;
    // }

    public int GetCheckedSpecialtyCountInArea(string areaName)
    {
        int areaColumnIndex = GetAreaColumnIndex(areaName);
        if (areaColumnIndex < 0)
        {
            return 0;
        }

        int checkedCount = 0;
        for (int row = 0; row < RowCount; row++)
        {
            SpecialtyItem item = specialtyItems[(areaColumnIndex * RowCount) + row];
            if (item != null && item.IsChecked)
            {
                checkedCount++;
            }
        }

        return checkedCount;
    }

    public void SetDefaultSkillNames(string[] names)
    {
        defaultSkillNames = names ?? new string[SpecialtyItemCount];
    }

    public void ApplyAreaNames(string[] names)
    {
        if (names == null)
        {
            return;
        }

        List<SpecialtyAreaInfo> areas = GetCuriosityAreasInVisualOrder();
        for (int i = 0; i < areas.Count && i < names.Length; i++)
        {
            TextMeshProUGUI nameText = FindAreaNameText(areas[i].transform);
            if (nameText != null)
            {
                nameText.text = names[i] ?? string.Empty;
            }
        }

        cachedAreas = null;
    }

    /// <summary>
    /// InsaneManager에서 호출하여 최대 선택 가능 특성 수를 지정합니다.
    /// </summary>
    public void SetCheckedSpecialtyMax(int max)
    {
        checkedSpecialty = Mathf.Max(1, max);
        CheckedSpecialtyChanged?.Invoke();
    }

    public void SetFearSpecialtyMax(int max)
    {
        checkedFear = Mathf.Max(1, max);

        // 현재 선택된 공포심이 새 최대치를 초과하면 뒤에서부터 제거
        while (fearSpecialtyNames.Count > checkedFear)
            fearSpecialtyNames.RemoveAt(fearSpecialtyNames.Count - 1);

        ApplySkillStates();
        UpdateFearSpecialtyText();
        FearSpecialtyChanged?.Invoke();
    }

    /// <summary>
    /// 특성 그리드의 수평 순환 여부를 런타임에 설정합니다.
    /// AbilitySpecialEffect.WrapHorizontally 어빌리티 획득/해제 시 InsaneManager가 호출합니다.
    /// </summary>
    public void SetWrapHorizontally(bool wrap)
    {
        if (wrapHorizontally == wrap)
            return;

        wrapHorizontally = wrap;
        RecalculateCounts(); // 이동 비용이 바뀌므로 난이도 재계산
    }

    public void SetCuriosityAreaName(string newCuriosityAreaName)
    {
        curiosityAreaName = newCuriosityAreaName ?? string.Empty;
        ApplyCuriosityBars();
        ApplySkillStates();
        RecalculateCounts();
    }

    // ─── Curiosity Dropdown ──────────────────────────────────────

    /// <summary>호기심 드롭다운 옵션을 갱신하고 selectedAreaName으로 선택값을 설정합니다.</summary>
    public void RefreshCuriosityDropdown(string selectedAreaName)
    {
        if (curiosityDropdown == null)
            return;

        List<string> options = new List<string> { NoneDropdownOptionText };
        List<string> areaNames = GetCuriosityAreaNames();
        for (int i = 0; i < areaNames.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(areaNames[i]))
                options.Add(areaNames[i]);
        }

        curiosityDropdown.ClearOptions();
        curiosityDropdown.AddOptions(options);

        int selectedIndex = 0;
        string normalized = NormalizeDropdownSelection(selectedAreaName);
        for (int i = 1; i < options.Count; i++)
        {
            if (options[i] == normalized)
            {
                selectedIndex = i;
                break;
            }
        }

        curiosityDropdown.SetValueWithoutNotify(selectedIndex);
        curiosityDropdown.RefreshShownValue();
    }

    private void HandleCuriosityDropdownChanged(int optionIndex)
    {
        string selected = GetDropdownOptionText(optionIndex);
        string normalizedAreaName = NormalizeDropdownSelection(selected);
        SetCuriosityAreaName(normalizedAreaName);
        CuriosityAreaChanged?.Invoke(normalizedAreaName);
    }

    private string GetDropdownOptionText(int optionIndex)
    {
        if (curiosityDropdown == null || optionIndex < 0 || optionIndex >= curiosityDropdown.options.Count)
            return string.Empty;
        return curiosityDropdown.options[optionIndex].text;
    }

    private string NormalizeDropdownSelection(string optionText)
    {
        if (string.IsNullOrWhiteSpace(optionText) || optionText == NoneDropdownOptionText)
            return string.Empty;
        return optionText;
    }

    // ─── Fear Specialty Text ─────────────────────────────────────

    private void UpdateFearSpecialtyText()
    {
        if (fearSpecialtyText == null)
            return;

        if (fearSpecialtyNames == null || fearSpecialtyNames.Count == 0)
        {
            fearSpecialtyText.text = string.Empty;
        }
        else
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < fearSpecialtyNames.Count; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append(fearSpecialtyNames[i]);
            }
            fearSpecialtyText.text = sb.ToString();
        }

        // 중첩 ContentSizeFitter는 자동으로 부모에 전파되지 않으므로
        // Text → 부모(Image) 순으로 직접 재계산
        LayoutRebuilder.ForceRebuildLayoutImmediate(fearSpecialtyText.rectTransform);
        RectTransform parentRect = fearSpecialtyText.transform.parent as RectTransform;
        if (parentRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
    }

    public void SetFearSpecialtyNames(List<string> newFearSpecialtyNames)
    {
        fearSpecialtyNames = newFearSpecialtyNames != null ? new List<string>(newFearSpecialtyNames) : new List<string>();
        ApplySkillStates();
        UpdateFearSpecialtyText();
    }

    [ContextMenu("Reset Custom Skill Names To Default")]
    public void ResetCustomSkillNamesToDefault()
    {
        EnsureSkillNameStorage();

        for (int i = 0; i < SpecialtyItemCount; i++)
        {
            customSkillNames[i] = GetDefaultSkillName(i);
        }

        ApplySkillNames();
        ApplySkillStates();
        ApplyCuriosityBars();
        RecalculateCounts();
        CheckedSpecialtyChanged?.Invoke();
    }

    public void SetCustomSkillName(int column, int row, string newSkillName)
    {
        int index = GetFlatIndex(column, row);
        if (index < 0)
        {
            return;
        }

        EnsureSkillNameStorage();
        customSkillNames[index] = newSkillName ?? string.Empty;

        if (useCustomSkillNames && specialtyItems[index] != null)
        {
            specialtyItems[index].SetSkillName(customSkillNames[index]);
        }
    }

    public void ApplyCustomSheetSaveData(CustomSheetSaveData saveData)
    {
        EnsureSkillNameStorage();

        if (saveData == null)
        {
            return;
        }

        if (saveData.areaNames != null)
        {
            ApplyAreaNames(saveData.areaNames);
        }

        if (saveData.skillNames != null)
        {
            for (int i = 0; i < SpecialtyItemCount; i++)
            {
                customSkillNames[i] = i < saveData.skillNames.Length ? saveData.skillNames[i] ?? string.Empty : string.Empty;
            }

            useCustomSkillNames = HasAnySkillName(saveData.skillNames);
            ApplySkillNames();
        }

        ApplySkillStates();
        ApplyCuriosityBars();
        RecalculateCounts();
    }

    public InsaneSpecialtyData CreateSpecialtySaveData()
    {
        EnsureSkillNameStorage();
        RecalculateCounts();

        InsaneSpecialtyData saveData = new InsaneSpecialtyData
        {
            areaNames = GetCurrentAreaNames(),
            curiosityAreaName = curiosityAreaName,
            fearSpecialtyNames = new List<string>(fearSpecialtyNames)
        };

        for (int i = 0; i < SpecialtyItemCount; i++)
        {
            int column = i / RowCount;
            int row = i % RowCount;
            SpecialtyItem item = specialtyItems[i];

            saveData.specialties.Add(new InsaneSpecialtyEntryData
            {
                column = column,
                row = row,
                specialtyName = item != null ? item.SkillName : GetSkillNameForIndex(i),
                difficulty = item != null ? item.Difficulty : SpecialtyItem.MaxDifficulty,
                isChecked = item != null && item.IsChecked
            });
        }

        return saveData;
    }

    public void ApplySpecialtySaveData(InsaneSpecialtyData saveData)
    {
        EnsureSkillNameStorage();

        if (saveData == null)
        {
            RefreshSkillNames();
            return;
        }

        curiosityAreaName = saveData.curiosityAreaName ?? string.Empty;
        fearSpecialtyNames = saveData.fearSpecialtyNames != null ? new List<string>(saveData.fearSpecialtyNames) : new List<string>();
        ApplyAreaNames(saveData.areaNames);

        InsaneSpecialtyEntryData[] entries = saveData.specialties != null
            ? saveData.specialties.ToArray()
            : System.Array.Empty<InsaneSpecialtyEntryData>();

        bool hasSavedSkillNames = false;
        for (int i = 0; i < entries.Length; i++)
        {
            int index = GetFlatIndex(entries[i].column, entries[i].row);
            if (index < 0)
            {
                continue;
            }

            customSkillNames[index] = entries[i].specialtyName ?? string.Empty;
            hasSavedSkillNames |= !string.IsNullOrWhiteSpace(customSkillNames[index]);
        }

        useCustomSkillNames = hasSavedSkillNames;
        ApplySkillNames();

        for (int i = 0; i < specialtyItems.Length; i++)
        {
            if (specialtyItems[i] != null)
            {
                specialtyItems[i].SetCheckedSilently(false);
            }
        }

        for (int i = 0; i < entries.Length; i++)
        {
            int index = GetFlatIndex(entries[i].column, entries[i].row);
            if (index >= 0 && specialtyItems[index] != null)
            {
                specialtyItems[index].SetCheckedSilently(entries[i].isChecked);
            }
        }

        ApplySkillStates();
        ApplyCuriosityBars();
        RecalculateCounts();
        UpdateFearSpecialtyText();
    }

    public void RefreshSkillNames()
    {
        ApplySkillNames();
        ApplySkillStates();
        ApplyCuriosityBars();
        RecalculateCounts();
    }

    public void RecalculateCounts()
    {
        for (int i = 0; i < specialtyItems.Length; i++)
        {
            if (specialtyItems[i] != null)
            {
                specialtyItems[i].SetDifficulty(SpecialtyItem.MaxDifficulty);
            }
        }

        for (int i = 0; i < specialtyItems.Length; i++)
        {
            SpecialtyItem sourceItem = specialtyItems[i];
            if (sourceItem == null || !sourceItem.IsChecked)
            {
                continue;
            }

            int sourceColumn = i / RowCount;
            int sourceRow = i % RowCount;
            ApplyCheckedItemInfluence(sourceColumn, sourceRow);
        }

    }

    private void RegisterItems()
    {
        for (int i = 0; i < specialtyItems.Length; i++)
        {
            if (specialtyItems[i] != null)
            {
                specialtyItems[i].Initialize(HandleSkillItemCheckedChanged, HandleSkillItemRightClicked);
            }
        }
    }

    private void HandleSkillItemCheckedChanged(SpecialtyItem changedItem, bool isChecked)
    {
        if (isChecked && GetCheckedSpecialtyCount() > checkedSpecialty)
        {
            changedItem.SetCheckedSilently(false);
        }

        RecalculateCounts();
        CheckedSpecialtyChanged?.Invoke();
    }

    private void HandleSkillItemRightClicked(SpecialtyItem item)
    {
        string name = item.SkillName;
        if (fearSpecialtyNames.Contains(name))
            fearSpecialtyNames.Remove(name);
        else if (fearSpecialtyNames.Count < checkedFear)
            fearSpecialtyNames.Add(name);

        ApplySkillStates();
        UpdateFearSpecialtyText();
        FearSpecialtyChanged?.Invoke();
    }

    private void ApplyCheckedItemInfluence(int sourceColumn, int sourceRow)
    {
        for (int column = 0; column < ColumnCount; column++)
        {
            for (int row = 0; row < RowCount; row++)
            {
                SpecialtyItem target = specialtyItems[(column * RowCount) + row];
                if (target == null)
                {
                    continue;
                }

                int horizontalCost = GetHorizontalTravelCost(sourceColumn, column);
                int verticalDistance = Mathf.Abs(row - sourceRow);
                int influencedCount =
                    SpecialtyItem.MinDifficulty +
                    horizontalCost +
                    (verticalDistance * VerticalStepCost);

                if (influencedCount < target.Difficulty)
                {
                    target.SetDifficulty(influencedCount);
                }
            }
        }
    }

    private int GetHorizontalTravelCost(int sourceColumn, int targetColumn)
    {
        if (sourceColumn == targetColumn)
        {
            return 0;
        }

        if (!wrapHorizontally)
        {
            int directDirection = targetColumn > sourceColumn ? 1 : -1;
            return GetDirectionalHorizontalCost(sourceColumn, targetColumn, directDirection);
        }

        int forwardCost = GetDirectionalHorizontalCost(sourceColumn, targetColumn, 1);
        int backwardCost = GetDirectionalHorizontalCost(sourceColumn, targetColumn, -1);
        return Mathf.Min(forwardCost, backwardCost);
    }

    private int GetDirectionalHorizontalCost(int sourceColumn, int targetColumn, int direction)
    {
        int currentColumn = sourceColumn;
        int totalCost = 0;

        while (currentColumn != targetColumn)
        {
            int nextColumn = currentColumn + direction;

            if (wrapHorizontally)
            {
                nextColumn = (nextColumn + ColumnCount) % ColumnCount;
            }

            totalCost += GetStepCost(currentColumn, nextColumn);
            currentColumn = nextColumn;
        }

        return totalCost;
    }

    private int GetStepCost(int fromColumn, int toColumn)
    {
        int curiosityColumnIndex = GetCuriosityColumnIndex();
        if (curiosityColumnIndex < 0)
        {
            return HorizontalStepCost;
        }

        if (IsCuriosityBorder(fromColumn, toColumn, curiosityColumnIndex))
        {
            return CuriosityHorizontalStepCost;
        }

        return HorizontalStepCost;
    }

    private bool IsCuriosityBorder(int fromColumn, int toColumn, int curiosityColumnIndex)
    {
        int leftColumn = curiosityColumnIndex - 1;
        int rightColumn = curiosityColumnIndex + 1;

        if (wrapHorizontally)
        {
            leftColumn = (leftColumn + ColumnCount) % ColumnCount;
            rightColumn = rightColumn % ColumnCount;
        }

        return IsSameBorder(fromColumn, toColumn, leftColumn, curiosityColumnIndex)
            || IsSameBorder(fromColumn, toColumn, curiosityColumnIndex, rightColumn);
    }

    private bool IsSameBorder(int fromColumn, int toColumn, int borderLeftColumn, int borderRightColumn)
    {
        if (borderLeftColumn < 0 || borderLeftColumn >= ColumnCount || borderRightColumn < 0 || borderRightColumn >= ColumnCount)
        {
            return false;
        }

        return (fromColumn == borderLeftColumn && toColumn == borderRightColumn)
            || (fromColumn == borderRightColumn && toColumn == borderLeftColumn);
    }

    public int GetCheckedSpecialtyCount()
    {
        int checkedItemCount = 0;

        for (int i = 0; i < specialtyItems.Length; i++)
        {
            if (specialtyItems[i] != null && specialtyItems[i].IsChecked)
            {
                checkedItemCount++;
            }
        }

        return checkedItemCount;
    }

    private void EnsureSkillNameStorage()
    {
        if (customSkillNames == null || customSkillNames.Length != SpecialtyItemCount)
        {
            string[] resizedNames = new string[SpecialtyItemCount];

            if (customSkillNames != null)
            {
                int copyCount = Mathf.Min(customSkillNames.Length, resizedNames.Length);
                for (int i = 0; i < copyCount; i++)
                {
                    resizedNames[i] = customSkillNames[i];
                }
            }

            customSkillNames = resizedNames;
        }
    }

    private void ApplySkillNames()
    {
        for (int i = 0; i < specialtyItems.Length; i++)
        {
            if (specialtyItems[i] == null)
            {
                continue;
            }

            specialtyItems[i].SetSkillName(GetSkillNameForIndex(i));
        }

        SkillNamesChanged?.Invoke();
    }

    public string[] GetCurrentAreaNames()
    {
        List<SpecialtyAreaInfo> areas = GetCuriosityAreasInVisualOrder();
        string[] names = new string[areas.Count];

        for (int i = 0; i < areas.Count; i++)
        {
            names[i] = areas[i].areaName ?? string.Empty;
        }

        return names;
    }

    /// <summary>
    /// 현재 커스텀 스킬 이름 배열을 반환합니다.
    /// useCustomSkillNames가 false이면 null을 반환합니다.
    /// </summary>
    public string[] GetCurrentCustomSkillNames()
    {
        if (!useCustomSkillNames)
            return null;

        EnsureSkillNameStorage();
        return (string[])customSkillNames.Clone();
    }

    private bool HasAnySkillName(string[] names)
    {
        if (names == null)
        {
            return false;
        }

        for (int i = 0; i < names.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(names[i]))
            {
                return true;
            }
        }

        return false;
    }

    private void ApplySkillStates()
    {
        for (int i = 0; i < specialtyItems.Length; i++)
        {
            SpecialtyItem item = specialtyItems[i];
            if (item == null)
            {
                continue;
            }

            item.SetState(GetStateForPosition(item.SkillName));
        }

    }

    private SpecialtyItem.SkillState GetStateForPosition(string specialtyName)
    {
        if (fearSpecialtyNames != null && fearSpecialtyNames.Contains(specialtyName))
        {
            return SpecialtyItem.SkillState.Fear;
        }

        return SpecialtyItem.SkillState.Normal;
    }

    private void ApplyCuriosityBars()
    {
        if (bars == null)
        {
            return;
        }

        for (int i = 0; i < bars.Length; i++)
        {
            if (bars[i] != null)
            {
                bars[i].color = normalBarColor;
            }
        }

        int curiosityColumnIndex = GetCuriosityColumnIndex();
        if (curiosityColumnIndex < 0)
        {
            return;
        }

        if (bars.Length == ColumnCount + 1)
        {
            SetBarColor(curiosityColumnIndex, curiosityBarColor);
            SetBarColor(curiosityColumnIndex + 1, curiosityBarColor);
            return;
        }

        SetBarColor(curiosityColumnIndex - 1, curiosityBarColor);
        SetBarColor(curiosityColumnIndex, curiosityBarColor);
    }

    private void SetBarColor(int barIndex, Color color)
    {
        if (bars == null || barIndex < 0 || barIndex >= bars.Length || bars[barIndex] == null)
        {
            return;
        }

        bars[barIndex].color = color;
    }

    private int GetCuriosityColumnIndex()
    {
        return GetAreaColumnIndex(curiosityAreaName);
    }

    private int GetAreaColumnIndex(string areaName)
    {
        if (string.IsNullOrWhiteSpace(areaName))
        {
            return -1;
        }

        List<string> areaNames = GetCuriosityAreaNames();
        for (int i = 0; i < areaNames.Count; i++)
        {
            if (areaNames[i] == areaName)
            {
                return i;
            }
        }

        return -1;
    }

    private TextMeshProUGUI FindAreaNameText(Transform specialtyArea)
    {
        Transform areaName = specialtyArea.Find("AreaName");
        if (areaName == null)
        {
            return null;
        }

        Transform text = areaName.Find("Text (TMP)");
        if (text == null)
        {
            return null;
        }

        return text.GetComponent<TextMeshProUGUI>();
    }

    private string GetSkillNameForIndex(int index)
    {
        if (index < 0 || index >= SpecialtyItemCount)
        {
            return string.Empty;
        }

        if (useCustomSkillNames && !string.IsNullOrWhiteSpace(customSkillNames[index]))
        {
            return customSkillNames[index];
        }

        return GetDefaultSkillName(index);
    }

    private string GetDefaultSkillName(int index)
    {
        if (defaultSkillNames == null || index < 0 || index >= defaultSkillNames.Length)
        {
            return string.Empty;
        }

        return defaultSkillNames[index] ?? string.Empty;
    }

    private int GetFlatIndex(int column, int row)
    {
        if (column < 0 || column >= ColumnCount || row < 0 || row >= RowCount)
        {
            return -1;
        }

        return (column * RowCount) + row;
    }
}
