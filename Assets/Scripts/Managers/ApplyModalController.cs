using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Michsky.UI.Dark;

/// <summary>
/// 범용 확인 모달.
/// onConfirm 콜백을 등록하거나 생략할 수 있으며, 확인 버튼 클릭 시 콜백을 호출한 뒤 모달을 닫습니다.
/// </summary>
public class ApplyModalController : MonoBehaviour
{
    [SerializeField] private ModalWindowManager modalWindow;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private Action confirmCallback;

    // ─── Unity 이벤트 ─────────────────────────────────────────

    private void Awake()
    {
        if (modalWindow == null)
            modalWindow = GetComponent<ModalWindowManager>();

        if (modalWindow == null)
            Debug.LogWarning("[ApplyModalController] Awake: ModalWindowManager 컴포넌트를 찾을 수 없음");

        if (confirmButton == null)
            Debug.LogWarning("[ApplyModalController] Awake: confirmButton이 null — 인스펙터 미할당");
    }

    private void OnEnable()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(Close);
    }

    private void OnDisable()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(OnConfirmClicked);

        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(Close);

        confirmCallback = null;
    }

    // ─── 외부 호출 ────────────────────────────────────────────

    /// <summary>
    /// 확인 모달을 엽니다.
    /// </summary>
    /// <param name="title">모달 제목</param>
    /// <param name="description">모달 설명</param>
    /// <param name="onConfirm">확인 버튼 클릭 시 실행할 콜백 (생략 가능)</param>
    public void Open(string title, string description, Action onConfirm = null)
    {
        confirmCallback = onConfirm;
        OpenModal(title, description);
    }

    public void Close()
    {
        if (modalWindow != null)
            modalWindow.ModalWindowOut();
    }

    // ─── 내부 처리 ────────────────────────────────────────────

    private void OnConfirmClicked()
    {
        Action callback = confirmCallback;
        confirmCallback = null;

        Close();
        callback?.Invoke();
    }

    private void OpenModal(string title, string description)
    {
        if (modalWindow == null)
        {
            Debug.LogWarning("[ApplyModalController] OpenModal: modalWindow가 null — 모달 열기 실패");
            return;
        }

        modalWindow.title       = title;
        modalWindow.description = description;
        modalWindow.UpdateUI();
        modalWindow.gameObject.SetActive(true);
        modalWindow.ModalWindowIn();
    }
}
