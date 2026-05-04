using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Michsky.UI.Dark;

/// <summary>
/// 성장 창 — 공적점 추가·누적 표시 및 후유증 판정(공포심 변동)을 담당합니다.
/// 공적점·공포심은 설정창의 기본 스탯(생명력·이성치)과 별도로 InsaneCharacterSheet.growth에 저장됩니다.
/// </summary>
public class GrowthWindow : MonoBehaviour
{
    // ─── 후유증 판정 결과 ─────────────────────────────────────

    private enum TraumaResult { Special, Success, Failure, Fumble }

    private static readonly int[] FearDelta =
    {
        -1, // Special
         0, // Success
        +1, // Failure
        +2, // Fumble
    };

    // ─── 직렬화 필드 ──────────────────────────────────────────

    [SerializeField] private InsaneManager insaneManager;
    [SerializeField] private ModalWindowManager modalWindowManager;


    [Header("Merit Points / 공적점")]
    [Tooltip("추가할 공적점을 입력하는 필드 (양수·음수 모두 가능)")]
    [SerializeField] private TMP_InputField addPointsInputField;

    [Header("Trauma Judgment / 후유증 판정")]
    [Tooltip("라디오 버튼 그룹. 비워두면 Awake에서 자동 생성합니다.")]
    [SerializeField] private ToggleGroup toggleGroup;
    [Tooltip("스페셜 — 공포심 -1")]
    [SerializeField] private Toggle specialToggle;
    [Tooltip("성공 — 공포심 변동 없음")]
    [SerializeField] private Toggle successToggle;
    [Tooltip("실패 — 공포심 +1")]
    [SerializeField] private Toggle failureToggle;
    [Tooltip("펌블 — 공포심 +2")]
    [SerializeField] private Toggle fumbleToggle;

    [Header("Button")]
    [Tooltip("클릭 시 ApplyModal을 열어 확인 후 적용합니다.")]
    [SerializeField] private Button applyButton;

    // ─── Unity 이벤트 ─────────────────────────────────────────

    private void Awake()
    {
        SetupToggleGroup();
    }

    private void OnEnable()
    {
        RefreshUI();

        if (applyButton != null)
            applyButton.onClick.AddListener(OnApplyButtonClicked);

        AddToggleListeners();
    }

    private void OnDisable()
    {
        if (applyButton != null)
            applyButton.onClick.RemoveListener(OnApplyButtonClicked);

        RemoveToggleListeners();
    }

    // ─── Toggle Group 초기화 ──────────────────────────────────

    private void SetupToggleGroup()
    {
        if (toggleGroup == null)
            toggleGroup = gameObject.AddComponent<ToggleGroup>();

        toggleGroup.allowSwitchOff = true;

        AssignToGroup(specialToggle);
        AssignToGroup(successToggle);
        AssignToGroup(failureToggle);
        AssignToGroup(fumbleToggle);
    }

    private void AssignToGroup(Toggle toggle)
    {
        if (toggle == null) return;
        toggle.group = toggleGroup;
        toggle.SetIsOnWithoutNotify(false);
    }

    private void AddToggleListeners()
    {
        if (specialToggle != null) specialToggle.onValueChanged.AddListener(OnToggleChanged);
        if (successToggle != null) successToggle.onValueChanged.AddListener(OnToggleChanged);
        if (failureToggle != null) failureToggle.onValueChanged.AddListener(OnToggleChanged);
        if (fumbleToggle  != null) fumbleToggle.onValueChanged.AddListener(OnToggleChanged);
    }

    private void RemoveToggleListeners()
    {
        if (specialToggle != null) specialToggle.onValueChanged.RemoveListener(OnToggleChanged);
        if (successToggle != null) successToggle.onValueChanged.RemoveListener(OnToggleChanged);
        if (failureToggle != null) failureToggle.onValueChanged.RemoveListener(OnToggleChanged);
        if (fumbleToggle  != null) fumbleToggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    private void OnToggleChanged(bool _)
    {
        RefreshApplyButton();
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
    /// 입력 필드를 초기화하고 토글 및 버튼 상태를 갱신합니다.
    /// </summary>
    public void RefreshUI()
    {
        if (addPointsInputField != null)
            addPointsInputField.SetTextWithoutNotify("0");

        toggleGroup?.SetAllTogglesOff(false);

        RefreshApplyButton();
    }

    // ─── 적용 버튼 활성화 제어 ───────────────────────────────

    private void RefreshApplyButton()
    {
        if (applyButton != null)
            applyButton.interactable = AnyToggleOn();
    }

    private bool AnyToggleOn()
    {
        if (specialToggle != null && specialToggle.isOn) return true;
        if (successToggle != null && successToggle.isOn) return true;
        if (failureToggle != null && failureToggle.isOn) return true;
        if (fumbleToggle  != null && fumbleToggle.isOn)  return true;
        return false;
    }

    // ─── applyButton 클릭 ─────────────────────────────────────

    /// <summary>
    /// applyButton 클릭 시 호출됩니다.
    /// Manager.ApplyModal이 존재하면 확인 모달을 열고, 확인 시 OnApplyClicked + Close를 실행합니다.
    /// Manager.ApplyModal이 없으면 즉시 OnApplyClicked를 실행합니다.
    /// </summary>
    private void OnApplyButtonClicked()
    {
        ApplyModalController modal = Manager.ApplyModal;
        if (modal != null)
        {
            modal.Open("성장 적용", "공적점 및 후유증 판정을 적용하시겠습니까?",
                () =>
                {
                    OnApplyClicked();
                    Close();
                });
        }
        else
        {
            OnApplyClicked();
        }
    }

    // ─── 실제 적용 로직 ──────────────────────────────────────

    /// <summary>공적점 추가 + 후유증 판정을 실행합니다.</summary>
    private void OnApplyClicked()
    {
        if (insaneManager == null)
            return;

        // ① 공적점 추가 — 입력값이 0이 아닌 경우에만 처리
        string raw = addPointsInputField != null ? addPointsInputField.text : string.Empty;
        if (int.TryParse(raw, out int amount) && amount != 0)
        {
            insaneManager.AddMeritPoints(amount);

            if (addPointsInputField != null)
                addPointsInputField.SetTextWithoutNotify("0");

            Debug.Log($"[GrowthWindow] 공적점 {amount:+0;-0} 추가 → 누적: {insaneManager.GetMeritPoints()}");
        }

        // ② 후유증 판정
        if (!TryGetSelectedTraumaResult(out TraumaResult result))
            return;

        int delta  = FearDelta[(int)result];
        int before = insaneManager.GetFearLevel();

        insaneManager.ChangeFearLevel(delta);

        int after = insaneManager.GetFearLevel();
        Debug.Log($"[GrowthWindow] 후유증 판정 — {result} (Δ{delta:+0;-0;0}) | {before} → {after}");
    }

    // ─── 유틸 ─────────────────────────────────────────────────

    private bool TryGetSelectedTraumaResult(out TraumaResult result)
    {
        if (specialToggle != null && specialToggle.isOn) { result = TraumaResult.Special; return true; }
        if (successToggle != null && successToggle.isOn) { result = TraumaResult.Success; return true; }
        if (failureToggle != null && failureToggle.isOn) { result = TraumaResult.Failure; return true; }
        if (fumbleToggle  != null && fumbleToggle.isOn)  { result = TraumaResult.Fumble;  return true; }
        result = default;
        return false;
    }
}
