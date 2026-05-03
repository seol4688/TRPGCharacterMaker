using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AbilitySelectWindow의 각 패널에 표시되는 어빌리티 한 행(行).
/// Setup 후 Unity 인스펙터의 Button.OnClick → OnSelectClicked() 연결.
/// </summary>
public class AbilitySelectEntry : MonoBehaviour
{
    [SerializeField] private Button          selectButton;
    [SerializeField] private TextMeshProUGUI libraryText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private TextMeshProUGUI specialtyText;
    [SerializeField] private TextMeshProUGUI effectText;
    [SerializeField] private TextMeshProUGUI conditionText;
    //[SerializeField] private TextMeshProUGUI statEffectText;

    [Header("현재 선택 하이라이트")]
    [Tooltip("선택 상태에 따라 색이 바뀔 이미지 배열")]
    [SerializeField] private Image[] colorTargets;
    [SerializeField] private Color   normalColor   = Color.white;
    [SerializeField] private Color   selectedColor = Color.white;

    private Action<AbilityData> onClicked;
    private AbilityData         abilityData;

    public AbilityData AbilityData => abilityData;

    // ─── 초기화 ──────────────────────────────────────────────

    /// <summary>
    /// <param name="data">표시할 어빌리티 데이터</param>
    /// <param name="isSelectable">선택 가능 여부 (false 이면 버튼 비활성화)</param>
    /// <param name="callback">선택 시 호출할 콜백</param>
    /// </summary>
    public void Setup(AbilityData data, bool isSelectable, Action<AbilityData> callback)
    {
        abilityData = data;
        onClicked   = callback;

        // ── [DEBUG 1] Setup 진입 확인 ──────────────────────────
        Debug.Log($"[AbilitySelectEntry] Setup: name={data?.name}, " +
                  $"selectButton={(selectButton != null ? "OK" : "NULL")}, " +
                  $"callback={(callback != null ? "OK" : "NULL")}");

        if (libraryText   != null) libraryText.text   = data.library          ?? string.Empty;
        if (nameText      != null) nameText.text      = data.name             ?? string.Empty;
        if (typeText      != null) typeText.text      = GetTypeText(data.type);
        if (specialtyText != null) specialtyText.text = GetSpecialtyText(data);
        if (effectText    != null) effectText.text    = data.effect           ?? string.Empty;
        if (conditionText != null) conditionText.text = data.acquireCondition ?? string.Empty;

        if (selectButton != null)
        {
            // 중복 등록 방지 후 코드로 리스너 추가 (인스펙터 연결 불필요)
            selectButton.onClick.RemoveListener(OnSelectClicked);
            selectButton.onClick.AddListener(OnSelectClicked);

            // ── [DEBUG 2] 리스너 등록 확인 ─────────────────────
            Debug.Log($"[AbilitySelectEntry] 버튼 리스너 등록 완료: name={data?.name}");
        }
        else
        {
            Debug.LogWarning($"[AbilitySelectEntry] selectButton이 NULL입니다 — 인스펙터에서 할당하세요. (name={data?.name})");
        }

        // 초기 하이라이트 초기화 (창이 닫혔다 다시 열릴 때 잔상 방지)
        SetCurrentSelection(false);

        // 선택 가능 여부에 따라 오브젝트 활성화/비활성화
        gameObject.SetActive(isSelectable);
    }

    /// <summary>열릴 때마다 선택 가능 여부를 갱신합니다. 불가능하면 오브젝트를 비활성화합니다.</summary>
    public void SetSelectable(bool isSelectable)
    {
        gameObject.SetActive(isSelectable);

        // ── [DEBUG 3] 가용 여부 갱신 확인 ─────────────────────
        Debug.Log($"[AbilitySelectEntry] SetSelectable: name={abilityData?.name}, active={isSelectable}");
    }

    /// <summary>
    /// 이 항목이 AbilityItem에서 현재 선택된 어빌리티와 일치하는지 표시합니다.
    /// true이면 colorTargets를 selectedColor로, false이면 normalColor로 되돌립니다.
    /// </summary>
    public void SetCurrentSelection(bool isCurrent)
    {
        if (colorTargets == null)
            return;

        Color c = isCurrent ? selectedColor : normalColor;
        for (int i = 0; i < colorTargets.Length; i++)
        {
            if (colorTargets[i] != null)
                colorTargets[i].color = c;
        }
    }

    /// <summary>버튼 클릭 시 호출됩니다 (코드 및 인스펙터 양쪽 동작).</summary>
    public void OnSelectClicked()
    {
        // ── [DEBUG 4] 클릭 진입 확인 ──────────────────────────
        Debug.Log($"[AbilitySelectEntry] OnSelectClicked: name={abilityData?.name}, " +
                  $"onClicked={(onClicked != null ? "OK" : "NULL")}");

        if (onClicked == null)
        {
            Debug.LogWarning("[AbilitySelectEntry] onClicked이 NULL — Setup이 호출되지 않았거나 콜백이 전달되지 않았습니다.");
            return;
        }

        onClicked.Invoke(abilityData);
    }

    // ─── 텍스트 변환 ─────────────────────────────────────────

    private static string GetTypeText(InsaneAbilityType type)
    {
        switch (type)
        {
            case InsaneAbilityType.Attack:    return "공격";
            case InsaneAbilityType.Support:   return "서포트";
            case InsaneAbilityType.Equipment: return "장비";
            default:                          return string.Empty;
        }
    }

    private static string GetSpecialtyText(AbilityData data)
    {
        switch (data.designatedSpecialtyType)
        {
            case DesignatedSpecialtyType.None:        return "없음";
            case DesignatedSpecialtyType.Variable:    return "가변";
            case DesignatedSpecialtyType.AnySpecialty: return "전체 중 하나";
            default:
                if (data.designatedEntries == null || data.designatedEntries.Count == 0)
                    return string.Empty;

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < data.designatedEntries.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    DesignatedSpecialtyEntry e = data.designatedEntries[i];
                    sb.Append(e.isAreaEntry ? $"[{e.value}] 중 하나" : e.value);
                }
                return sb.ToString();
        }
    }

    private static string GetStatEffectText(AbilityStatEffect effect)
    {
        if (effect == null || effect.IsEmpty)
            return string.Empty;

        StringBuilder sb = new StringBuilder();
        if (effect.lifeBonus      != 0) sb.Append($"생명력 {(effect.lifeBonus      > 0 ? "+" : "")}{effect.lifeBonus} ");
        if (effect.sanityBonus    != 0) sb.Append($"이성치 {(effect.sanityBonus    > 0 ? "+" : "")}{effect.sanityBonus} ");
        if (effect.specialtyBonus != 0) sb.Append($"특성 수 {(effect.specialtyBonus > 0 ? "+" : "")}{effect.specialtyBonus}");
        return sb.ToString().Trim();
    }
}
