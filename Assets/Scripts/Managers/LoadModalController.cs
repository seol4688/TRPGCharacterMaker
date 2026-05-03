using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Michsky.UI.Dark;

public class LoadModalController : MonoBehaviour
{
    public struct CharacterLoadEntry
    {
        public string saveName;
        public string characterName;
        public string ruleName;
        public string avatarImagePath;
        public System.DateTime? creationDate;
    }

    [SerializeField] private ModalWindowManager modalWindow;
    [SerializeField] protected Transform fileListParent;
    [SerializeField] protected TextMeshProUGUI emptyListText;

    [Header("캐릭터 불러오기")]
    [SerializeField] private LoadItem_Character characterPrefab;

    [Header("사운드")]
    [Tooltip("UIElementSound에 할당할 AudioSource.\n비워두면 씬에서 'UI Audio' 이름으로 자동 탐색합니다.")]
    [SerializeField] private AudioSource uiAudioSource;

    protected Action<string> selectCallback;

    protected virtual void Awake()
    {
        if (modalWindow == null)
        {
            modalWindow = GetComponent<ModalWindowManager>();
        }

        if (modalWindow == null)
        {
            Debug.LogWarning("[LoadModalController] Awake: ModalWindowManager 컴포넌트를 찾을 수 없음");
        }

        if (fileListParent == null)
        {
            Debug.LogWarning("[LoadModalController] Awake: fileListParent가 null — 인스펙터 미할당");
        }

        
    }

    private void OnDisable()
    {
        selectCallback = null;
        ClearFileButtons();
    }

    public void OpenCharacter(string title, string description, CharacterLoadEntry[] entries, Action<string> onSelect, Action<string> onDelete = null)
    {
        Debug.Log($"[LoadModalController] OpenCharacter 호출됨 — 항목 {entries?.Length ?? 0}개");

        selectCallback = onSelect;
        ClearFileButtons();

        bool hasEntries = entries != null && entries.Length > 0;
        if (emptyListText != null)
            emptyListText.gameObject.SetActive(!hasEntries);

        if (hasEntries && fileListParent != null && characterPrefab != null)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                CharacterLoadEntry entry = entries[i];
                LoadItem_Character item = Instantiate(characterPrefab, fileListParent);
                item.gameObject.SetActive(true);
                item.Setup(entry.characterName, entry.ruleName, entry.avatarImagePath, entry.creationDate);
                item.SetAudioSource(uiAudioSource);
                item.SetCallback(() =>
                {
                    Debug.Log($"[LoadModalController] 캐릭터 선택됨 — saveName: '{entry.saveName}'");
                    Action<string> callback = selectCallback;
                    selectCallback = null;
                    callback?.Invoke(entry.saveName);
                    Close();
                });
                item.SetDeleteCallback(() =>
                {
                    Debug.Log($"[LoadModalController] 캐릭터 삭제됨 — saveName: '{entry.saveName}'");
                    onDelete?.Invoke(entry.saveName);
                    Destroy(item.gameObject);
                    RefreshEmptyText();
                });
            }
        }
        else if (hasEntries)
        {
            if (fileListParent == null) Debug.LogWarning("[LoadModalController] fileListParent가 null — 항목 생성 불가");
            if (characterPrefab == null) Debug.LogWarning("[LoadModalController] characterData가 null — 항목 생성 불가");
        }

        if (characterPrefab != null)
            characterPrefab.gameObject.SetActive(false);

        OpenModal(title, description);
    }

    public void Close()
    {
        Debug.Log("[LoadModalController] Close 호출됨");

        if (modalWindow != null)
        {
            modalWindow.ModalWindowOut();
        }
    }

    protected void OpenModal(string title, string description)
    {
        if (modalWindow == null)
        {
            Debug.LogWarning("[LoadModalController] OpenModal: modalWindow가 null — 모달 열기 실패");
            return;
        }

        Debug.Log("[LoadModalController] OpenModal: ModalWindowIn 호출");
        modalWindow.title = title;
        modalWindow.description = description;
        modalWindow.UpdateUI();
        modalWindow.gameObject.SetActive(true);
        modalWindow.ModalWindowIn();
    }

    protected void RefreshEmptyText()
    {
        if (emptyListText == null || fileListParent == null)
            return;

        int itemCount = 0;
        for (int i = 0; i < fileListParent.childCount; i++)
        {
            Transform child = fileListParent.GetChild(i);
            //if (fileButtonPrefab != null && child == fileButtonPrefab.transform) continue;
            if (characterPrefab != null && child == characterPrefab.transform) continue;
            itemCount++;
        }

        emptyListText.gameObject.SetActive(itemCount == 0);
    }

    protected void ClearFileButtons()
    {
        if (fileListParent == null)
            return;

        for (int i = fileListParent.childCount - 1; i >= 0; i--)
        {
            Transform child = fileListParent.GetChild(i);
            //if (fileButtonPrefab != null && child == fileButtonPrefab.transform) continue;
            if (characterPrefab != null && child == characterPrefab.transform) continue;
            Destroy(child.gameObject);
        }
    }

    protected void SetButtonText(Button button, string text)
    {
        TextMeshProUGUI tmpText = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmpText != null)
        {
            tmpText.text = text;
        }

        ButtonManager buttonManager = button.GetComponent<ButtonManager>();
        if (buttonManager != null)
        {
            buttonManager.buttonText = text;
            buttonManager.UpdateUI();
        }
    }
}
