using System;
using System.Collections.Generic;
using UnityEngine;

public class LoadModalController_Insane : LoadModalController
{
    [Header("커스텀 특기 불러오기")]
    [SerializeField] private LoadItem_CustomSpecialty customSpecialtyPrefab;

    protected override void Awake()
    {
        base.Awake();

        if (customSpecialtyPrefab == null)
            Debug.LogWarning("[LoadModalController_Insane] Awake: customSpecialtyPrefab이 null — 인스펙터 미할당");
    }

    public void Open(string title, string description, string[] saveNames, Action<string> onSelect, Action<string> onDelete = null)
    {
        Debug.Log($"[LoadModalController_Insane] Open 호출됨 — 저장 파일 {saveNames?.Length ?? 0}개");

        selectCallback = onSelect;
        ClearFileButtons();

        bool hasSaveFiles = saveNames != null && saveNames.Length > 0;
        if (emptyListText != null)
            emptyListText.gameObject.SetActive(!hasSaveFiles);

        if (!hasSaveFiles)
        {
            Debug.Log("[LoadModalController_Insane] 저장 파일 없음 — 빈 목록으로 모달 열기");
            OpenModal(title, description);
            return;
        }

        if (fileListParent == null || customSpecialtyPrefab == null)
        {
            if (fileListParent == null)       Debug.LogWarning("[LoadModalController_Insane] fileListParent가 null — 항목 생성 불가");
            if (customSpecialtyPrefab == null) Debug.LogWarning("[LoadModalController_Insane] customSpecialtyPrefab이 null — 항목 생성 불가");
            OpenModal(title, description);
            return;
        }

        // 즐겨찾기 상태 로드 후 즐겨찾기 우선 정렬
        DataManager dataManager = Manager.Data;
        List<(string saveName, bool isFavorite)> entries = new List<(string, bool)>(saveNames.Length);
        for (int i = 0; i < saveNames.Length; i++)
        {
            bool isFav = dataManager != null && dataManager.GetInsaneCustomSheetFavorite(saveNames[i]);
            entries.Add((saveNames[i], isFav));
        }
        entries.Sort((a, b) => b.isFavorite.CompareTo(a.isFavorite));

        for (int i = 0; i < entries.Count; i++)
        {
            string saveName   = entries[i].saveName;
            bool   isFavorite = entries[i].isFavorite;

            LoadItem_CustomSpecialty item = Instantiate(customSpecialtyPrefab, fileListParent);
            item.gameObject.SetActive(true);
            item.Setup(saveName, isFavorite);
            item.SetCallback(() =>
            {
                Debug.Log($"[LoadModalController_Insane] 파일 선택됨 — saveName: '{saveName}'");
                Action<string> callback = selectCallback;
                selectCallback = null;
                callback?.Invoke(saveName);
                Close();
            });
            item.SetFavoriteCallback(isFav =>
            {
                Debug.Log($"[LoadModalController_Insane] 즐겨찾기 변경 — saveName: '{saveName}', isFavorite: {isFav}");
                dataManager?.SetInsaneCustomSheetFavorite(saveName, isFav);
                ResortByFavorite();
            });
            item.SetDeleteCallback(() =>
            {
                Debug.Log($"[LoadModalController_Insane] 커스텀 특성 삭제됨 — saveName: '{saveName}'");
                onDelete?.Invoke(saveName);
                Destroy(item.gameObject);
                RefreshEmptyText();
            });
        }

        customSpecialtyPrefab.gameObject.SetActive(false);
        OpenModal(title, description);
    }

    // 즐겨찾기 On 아이템을 상단으로 재정렬
    private void ResortByFavorite()
    {
        if (fileListParent == null)
            return;

        List<Transform> favorites = new List<Transform>();
        List<Transform> others    = new List<Transform>();

        for (int i = 0; i < fileListParent.childCount; i++)
        {
            Transform child = fileListParent.GetChild(i);
            if (customSpecialtyPrefab != null && child == customSpecialtyPrefab.transform)
                continue;

            LoadItem_CustomSpecialty item = child.GetComponent<LoadItem_CustomSpecialty>();
            if (item == null)
                continue;

            if (item.IsFavorite) favorites.Add(child);
            else                 others.Add(child);
        }

        int siblingIndex = 0;
        for (int i = 0; i < favorites.Count; i++)
            favorites[i].SetSiblingIndex(siblingIndex++);
        for (int i = 0; i < others.Count; i++)
            others[i].SetSiblingIndex(siblingIndex++);
    }
}
