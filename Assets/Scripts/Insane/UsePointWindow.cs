using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Michsky.UI.Dark;

/// <summary>
/// 공적점 사용 창 — 어빌리티 습득, 공포심 극복, 경제력 상승을 담당합니다.
/// </summary>
public class UsePointWindow : MonoBehaviour
{
    // ─── 직렬화 필드 ──────────────────────────────────────────

    [SerializeField] private InsaneManager     insaneManager;
    [SerializeField] private ModalWindowManager modalWindowManager;

    [Header("Merit Points Display")]
    [Tooltip("남은 공적점을 표시하는 텍스트")]
    [SerializeField] private TextMeshProUGUI meritPointsText;

    [Header("Ability Acquisition / 어빌리티 습득")]
    [Tooltip("어빌리티 습득 버튼")]
    [SerializeField] private Button          abilityAcquisitionButton;
    [Tooltip("어빌리티 습득 비용을 표시하는 텍스트 (선택 가능 어빌리티 수 × 4)")]
    [SerializeField] private TextMeshProUGUI abilityCostText;

    [Header("Fear Overcome / 공포심 극복")]
    [Tooltip("공포심 극복 버튼")]
    [SerializeField] private Button          fearOvercomeButton;
    [Tooltip("공포심 극복 비용을 표시하는 텍스트 (고정 5)")]
    [SerializeField] private TextMeshProUGUI fearCostText;

    [Header("Economy Rise / 경제력 상승")]
    [Tooltip("경제력 상승 버튼")]
    [SerializeField] private Button          economyRiseButton;
    [Tooltip("경제력 상승 비용을 표시하는 텍스트 (고정 2)")]
    [SerializeField] private TextMeshProUGUI economyCostText;

    // ─── Unity 이벤트 ─────────────────────────────────────────

    private void OnEnable()
    {
        RefreshUI();

        if (abilityAcquisitionButton != null)
            abilityAcquisitionButton.onClick.AddListener(OnAbilityAcquisitionClicked);
        if (fearOvercomeButton != null)
            fearOvercomeButton.onClick.AddListener(OnFearOvercomeClicked);
        if (economyRiseButton != null)
            economyRiseButton.onClick.AddListener(OnEconomyRiseClicked);
    }

    private void OnDisable()
    {
        if (abilityAcquisitionButton != null)
            abilityAcquisitionButton.onClick.RemoveListener(OnAbilityAcquisitionClicked);
        if (fearOvercomeButton != null)
            fearOvercomeButton.onClick.RemoveListener(OnFearOvercomeClicked);
        if (economyRiseButton != null)
            economyRiseButton.onClick.RemoveListener(OnEconomyRiseClicked);
    }

    // ─── 외부 호출 : 열기·닫기 ───────────────────────────────

    public void Open()
    {
        RefreshUI();

        if (modalWindowManager != null)
            modalWindowManager.ModalWindowIn();
    }

    public void Close()
    {
        if (modalWindowManager != null)
            modalWindowManager.ModalWindowOut();
    }

    // ─── 외부 호출 : UI 갱신 ─────────────────────────────────

    /// <summary>
    /// InsaneManager에서 캐릭터 시트가 교체된 뒤 호출합니다.
    /// 공적점 표시와 버튼 상태를 최신화합니다.
    /// </summary>
    public void RefreshUI()
    {
        RefreshMeritPointsText();
        RefreshButtons(); // 비용 텍스트 + 이유 표시 + 버튼 활성화를 한 번에 처리
    }

    // ─── 버튼 클릭 핸들러 ─────────────────────────────────────

    private void OnAbilityAcquisitionClicked()
    {
        int cost = insaneManager != null ? insaneManager.GetAbilityAcquisitionCost() : 0;

        ApplyModalController modal = Manager.ApplyModal;
        if (modal != null)
        {
            modal.Open(
                "어빌리티 습득",
                $"공적점 {cost}을 소비하여 어빌리티 슬롯을 1개 늘리겠습니까?",
                () => { insaneManager?.TryAcquireAbilitySlot(); RefreshUI(); });
        }
        else
        {
            insaneManager?.TryAcquireAbilitySlot();
            RefreshUI();
        }
    }

    private void OnFearOvercomeClicked()
    {
        int cost = InsaneManager.FearOvercomeCost;

        ApplyModalController modal = Manager.ApplyModal;
        if (modal != null)
        {
            modal.Open(
                "공포심 극복",
                $"공적점 {cost}을 소비하여 공포심을 1개 제거하시겠습니까?",
                () => { insaneManager?.TryOvercomeFear(); RefreshUI(); });
        }
        else
        {
            insaneManager?.TryOvercomeFear();
            RefreshUI();
        }
    }

    private void OnEconomyRiseClicked()
    {
        int cost = InsaneManager.EconomyRiseCost;

        ApplyModalController modal = Manager.ApplyModal;
        if (modal != null)
        {
            modal.Open(
                "경제력 상승",
                $"공적점 {cost}을 소비하여 소지 아이템 갯수를 1개 늘리겠습니까?",
                () => { insaneManager?.TryRiseEconomy(); RefreshUI(); });
        }
        else
        {
            insaneManager?.TryRiseEconomy();
            RefreshUI();
        }
    }

    // ─── 내부 UI 갱신 ─────────────────────────────────────────

    private void RefreshMeritPointsText()
    {
        if (meritPointsText == null) return;
        int points = insaneManager != null ? insaneManager.GetMeritPoints() : 0;
        meritPointsText.text = points.ToString();
    }

    /// <summary>
    /// 각 기능의 버튼 활성화 여부를 판단하고,
    /// 비용 텍스트에 비활성화 이유를 붉은 글씨로 함께 표시합니다.
    /// </summary>
    private void RefreshButtons()
    {
        if (insaneManager == null)
        {
            SetInteractable(abilityAcquisitionButton, false);
            SetInteractable(fearOvercomeButton,       false);
            SetInteractable(economyRiseButton,        false);
            SetCostText(abilityCostText, 0,                               "※ 공적점이 부족합니다.");
            SetCostText(fearCostText,    InsaneManager.FearOvercomeCost,  "※ 공적점이 부족합니다.");
            SetCostText(economyCostText, InsaneManager.EconomyRiseCost,   "※ 공적점이 부족합니다.");
            return;
        }

        int points = insaneManager.GetMeritPoints();

        // ── 어빌리티 습득 ──────────────────────────────────
        int  abilityCost       = insaneManager.GetAbilityAcquisitionCost();
        bool canAcquireAbility = points >= abilityCost;
        SetInteractable(abilityAcquisitionButton, canAcquireAbility);
        SetCostText(abilityCostText, abilityCost,
            canAcquireAbility ? null : "※ 공적점이 부족합니다.");

        // ── 공포심 극복 ────────────────────────────────────
        bool hasPointsForFear = points >= InsaneManager.FearOvercomeCost;
        bool fearAboveMin     = insaneManager.GetFearLevel() > InsaneGrowthData.MinFearLevel;
        bool canOvercomeFear  = hasPointsForFear && fearAboveMin;
        SetInteractable(fearOvercomeButton, canOvercomeFear);
        string fearReason = null;
        if (!fearAboveMin)          fearReason = "※ 공포심 최솟값은 1입니다.";
        else if (!hasPointsForFear) fearReason = "※ 공적점이 부족합니다.";
        SetCostText(fearCostText, InsaneManager.FearOvercomeCost, fearReason);

        // ── 경제력 상승 ────────────────────────────────────
        bool canRiseEconomy = points >= InsaneManager.EconomyRiseCost;
        SetInteractable(economyRiseButton, canRiseEconomy);
        SetCostText(economyCostText, InsaneManager.EconomyRiseCost,
            canRiseEconomy ? null : "※ 공적점이 부족합니다.");
    }

    /// <summary>
    /// 비용 텍스트를 설정합니다.
    /// reason이 null이 아니면 기본 텍스트 뒤에 붉은 글씨로 이유를 추가합니다.
    /// </summary>
    private static void SetCostText(TextMeshProUGUI text, int cost, string reason)
    {
        if (text == null) return;
        string baseText = $"비용 : {cost}점";
        text.text = string.IsNullOrEmpty(reason)
            ? baseText
            : $"{baseText}  <color=red>{reason}</color>";
    }

    private static void SetInteractable(Button button, bool value)
    {
        if (button != null) button.interactable = value;
    }
}
