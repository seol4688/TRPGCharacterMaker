using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Michsky.UI.Dark;

public class SaveModalController : MonoBehaviour
{
    [SerializeField] private ModalWindowManager modalWindow;
    [SerializeField] private TMP_InputField fileNameInputField;
    [SerializeField] private Button confirmButton;

    private Action<string> confirmCallback;

    private void Awake()
    {
        if (modalWindow == null)
        {
            modalWindow = GetComponent<ModalWindowManager>();
        }

        if (modalWindow == null)
        {
            Debug.LogWarning("[SaveModalController] Awake: ModalWindowManager 컴포넌트를 찾을 수 없음");
        }

        if (fileNameInputField == null)
        {
            Debug.LogWarning("[SaveModalController] Awake: fileNameInputField가 null — 인스펙터 미할당");
        }

        if (confirmButton == null)
        {
            Debug.LogWarning("[SaveModalController] Awake: confirmButton이 null — 인스펙터 미할당");
        }
    }

    private void OnEnable()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(ConfirmInput);
        }

        if (fileNameInputField != null)
        {
            fileNameInputField.onValueChanged.AddListener(HandleFileNameChanged);
        }

        RefreshConfirmButtonState();
    }

    private void OnDisable()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(ConfirmInput);
        }

        if (fileNameInputField != null)
        {
            fileNameInputField.onValueChanged.RemoveListener(HandleFileNameChanged);
        }

        confirmCallback = null;
    }

    public void Open(string title, string description, Action<string> onConfirm)
    {
        Debug.Log("[SaveModalController] Open 호출됨");

        confirmCallback = onConfirm;

        if (fileNameInputField != null)
        {
            fileNameInputField.SetTextWithoutNotify(string.Empty);
            fileNameInputField.Select();
            fileNameInputField.ActivateInputField();
        }

        RefreshConfirmButtonState();
        OpenModal(title, description);
    }

    public void Close()
    {
        Debug.Log("[SaveModalController] Close 호출됨");

        if (modalWindow != null)
        {
            modalWindow.ModalWindowOut();
        }
    }

    private void ConfirmInput()
    {
        Debug.Log("[SaveModalController] ConfirmInput 호출됨");

        if (confirmCallback == null)
        {
            Debug.LogWarning("[SaveModalController] confirmCallback이 null — 콜백 미등록 상태");
            return;
        }

        if (!HasValidFileName())
        {
            Debug.LogWarning("[SaveModalController] 유효하지 않은 파일 이름 — 입력 필드 확인 필요");
            return;
        }

        string fileName = fileNameInputField != null ? fileNameInputField.text : string.Empty;
        Debug.Log($"[SaveModalController] 확인 — fileName: '{fileName}'");

        Action<string> callback = confirmCallback;
        confirmCallback = null;
        callback.Invoke(fileName);
        Close();
    }

    private void HandleFileNameChanged(string fileName)
    {
        RefreshConfirmButtonState();
    }

    private void RefreshConfirmButtonState()
    {
        if (confirmButton != null)
        {
            confirmButton.interactable = HasValidFileName();
        }
    }

    private bool HasValidFileName()
    {
        return fileNameInputField != null && !string.IsNullOrWhiteSpace(fileNameInputField.text);
    }

    private void OpenModal(string title, string description)
    {
        if (modalWindow == null)
        {
            Debug.LogWarning("[SaveModalController] OpenModal: modalWindow가 null — 모달 열기 실패");
            return;
        }

        Debug.Log("[SaveModalController] OpenModal: ModalWindowIn 호출");
        modalWindow.title = title;
        modalWindow.description = description;
        modalWindow.UpdateUI();
        modalWindow.gameObject.SetActive(true);
        modalWindow.ModalWindowIn();
    }
}
