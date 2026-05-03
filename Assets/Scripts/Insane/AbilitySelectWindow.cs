using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Michsky.UI.Dark;

/// <summary>
/// 어빌리티 선택 창.
/// 공식·비공식·이시키 3개 패널에 어빌리티를 나열합니다.
///
/// 사용 순서:
///   1. Ability.Start() → Initialize()  : 패널 항목 생성 (1회)
///   2. 버튼 클릭       → Open()         : 가용 여부 갱신 후 창 표시
/// </summary>
public class AbilitySelectWindow : MonoBehaviour
{
    [Header("Michsky UI")]
    [SerializeField] private ModalWindowManager modalWindow;
    [SerializeField] private MainPanelManager   mainPanelManager;

    [Header("패널 컨테이너 (스크롤 뷰 Content)")]
    [SerializeField] private Transform officialContainer;
    [SerializeField] private Transform unofficialContainer;
    [SerializeField] private Transform ishikiContainer;

    [Header("프리팹")]
    [SerializeField] private AbilitySelectEntry defultPrefab;
    [SerializeField] private AbilitySelectEntry ishikiPrefab;

    [Header("사운드")]
    [Tooltip("UIElementSound에 할당할 AudioSource.\n비워두면 씬에서 'UI Audio' 이름으로 자동 탐색합니다.")]
    [SerializeField] private AudioSource uiAudioSource;

    private readonly List<AbilitySelectEntry> officialEntries   = new List<AbilitySelectEntry>();
    private readonly List<AbilitySelectEntry> unofficialEntries = new List<AbilitySelectEntry>();
    private readonly List<AbilitySelectEntry> ishikiEntries     = new List<AbilitySelectEntry>();

    private bool                initialized = false;
    private Action<AbilityData> onSelected;

    // ─── 초기화 (Ability.Start에서 1회 호출) ─────────────────

    /// <summary>
    /// 창을 열기 전 Ability.Start()에서 호출합니다.
    /// 세 패널의 항목을 미리 생성하고 ContentSizeFitter를 강제 재계산합니다.
    /// </summary>
    public void Initialize(
        List<AbilityData> official,
        List<AbilityData> unofficial,
        List<AbilityData> ishiki)
    {
        if (initialized)
            return;

        BuildPanel(officialContainer,   official,   officialEntries);
        BuildPanel(unofficialContainer, unofficial, unofficialEntries);
        BuildPanel_Ishiki(ishikiContainer,     ishiki,     ishikiEntries);
        initialized = true;
    }

    // ─── 열기 / 닫기 ─────────────────────────────────────────

    /// <summary>데이터 없이 창만 엽니다 (인스펙터 버튼 등에서 호출).</summary>
    public void Open()
    {
        modalWindow.ModalWindowIn();
        mainPanelManager.OpenFirstTab();
    }

    /// <summary>
    /// Ability.cs에서 호출. 가용 여부와 현재 선택 하이라이트를 갱신한 뒤 창을 표시합니다.
    /// Initialize()가 먼저 호출되어 있어야 합니다.
    /// </summary>
    /// <param name="currentSelectedName">AbilityItem에 현재 선택된 어빌리티 이름. 해당 항목을 하이라이트합니다.</param>
    public void Open(
        List<AbilityData>   available,
        Action<AbilityData> callback,
        string              currentSelectedName = "")
    {
        onSelected = callback;

        RefreshAvailability(officialEntries,   available);
        RefreshAvailability(unofficialEntries, available);
        RefreshAvailability(ishikiEntries,     available);

        RefreshCurrentSelection(officialEntries,   currentSelectedName);
        RefreshCurrentSelection(unofficialEntries, currentSelectedName);
        RefreshCurrentSelection(ishikiEntries,     currentSelectedName);

        modalWindow.ModalWindowIn();
        mainPanelManager.OpenFirstTab();
    }

    /// <summary>
    /// 이전 Open(official, unofficial, ishiki, available, callback) 시그니처 호환용.
    /// Initialize()가 필요한 시점에 이미 호출된 경우라면 available과 callback만 사용합니다.
    /// </summary>
    public void Open(
        List<AbilityData>   official,
        List<AbilityData>   unofficial,
        List<AbilityData>   ishiki,
        List<AbilityData>   available,
        Action<AbilityData> callback,
        string              currentSelectedName = "")
    {
        // 아직 초기화되지 않았다면 지금 수행 (fallback)
        if (!initialized)
        {
            BuildPanel(officialContainer,   official,   officialEntries);
            BuildPanel(unofficialContainer, unofficial, unofficialEntries);
            BuildPanel_Ishiki(ishikiContainer,     ishiki,     ishikiEntries);
            initialized = true;
        }

        Open(available, callback, currentSelectedName);
    }

    public void Close()
    {
        modalWindow.ModalWindowOut();
    }

    // ─── 패널 최초 생성 ──────────────────────────────────────

    private void BuildPanel(
        Transform                container,
        List<AbilityData>        abilities,
        List<AbilitySelectEntry> entries)
    {
        if (container == null || defultPrefab == null)
            return;

        entries.Clear();

        if (abilities == null)
            return;

        AudioSource audio = GetUiAudioSource();

        for (int i = 0; i < abilities.Count; i++)
        {
            AbilityData data = abilities[i];
            if (data == null)
                continue;

            AbilitySelectEntry entry = Instantiate(defultPrefab, container);
            AssignAudioSource(entry, audio);
            entry.Setup(data, false, OnEntryClicked);
            entries.Add(entry);
        }
    }

    private void BuildPanel_Ishiki(
        Transform                container,
        List<AbilityData>        abilities,
        List<AbilitySelectEntry> entries)
    {
        if (container == null || ishikiPrefab == null)
            return;

        entries.Clear();

        if (abilities == null)
            return;

        AudioSource audio = GetUiAudioSource();

        for (int i = 0; i < abilities.Count; i++)
        {
            AbilityData data = abilities[i];
            if (data == null)
                continue;

            AbilitySelectEntry entry = Instantiate(ishikiPrefab, container);
            AssignAudioSource(entry, audio);
            entry.Setup(data, false, OnEntryClicked);
            entries.Add(entry);
        }
    }

    // ─── 사운드 할당 ─────────────────────────────────────────

    /// <summary>
    /// uiAudioSource가 인스펙터에서 할당되어 있으면 그것을 반환합니다.
    /// 없으면 씬에서 'UI Audio' 이름의 GameObject에 붙은 AudioSource를 탐색해 캐시합니다.
    /// </summary>
    private AudioSource GetUiAudioSource()
    {
        if (uiAudioSource != null)
            return uiAudioSource;

        GameObject found = GameObject.Find("UI Audio");
        if (found != null)
            uiAudioSource = found.GetComponent<AudioSource>();

        return uiAudioSource;
    }

    /// <summary>entry 하위의 모든 UIElementSound에 audioSource를 할당합니다.</summary>
    private static void AssignAudioSource(AbilitySelectEntry entry, AudioSource audio)
    {
        if (entry == null || audio == null)
            return;

        UIElementSound[] sounds = entry.GetComponentsInChildren<UIElementSound>(true);
        for (int i = 0; i < sounds.Length; i++)
            sounds[i].audioSource = audio;
    }

    // ─── 열 때마다 가용 여부 / 현재 선택 갱신 ──────────────────

    private static void RefreshAvailability(
        List<AbilitySelectEntry> entries,
        List<AbilityData>        available)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null)
                entries[i].SetSelectable(IsAvailable(entries[i].AbilityData, available));
        }
    }

    /// <summary>
    /// currentSelectedName과 이름이 일치하는 항목만 selectedColor로 하이라이트하고
    /// 나머지는 normalColor로 되돌립니다.
    /// </summary>
    private static void RefreshCurrentSelection(
        List<AbilitySelectEntry> entries,
        string                   currentSelectedName)
    {
        bool hasSelection = !string.IsNullOrEmpty(currentSelectedName);

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] == null)
                continue;

            bool isCurrent = hasSelection
                && entries[i].AbilityData != null
                && entries[i].AbilityData.name == currentSelectedName;

            entries[i].SetCurrentSelection(isCurrent);
        }
    }

    private static bool IsAvailable(AbilityData data, List<AbilityData> available)
    {
        if (data == null || available == null)
            return false;

        for (int i = 0; i < available.Count; i++)
        {
            if (available[i] != null && available[i].name == data.name)
                return true;
        }

        return false;
    }

    private void OnEntryClicked(AbilityData data)
    {
        // ── [DEBUG 5] 창 콜백 수신 확인 ───────────────────────
        Debug.Log($"[AbilitySelectWindow] OnEntryClicked: name={data?.name}, " +
                  $"onSelected={(onSelected != null ? "OK" : "NULL")}");

        if (onSelected == null)
        {
            Debug.LogWarning("[AbilitySelectWindow] onSelected이 NULL — Open(callback) 없이 창이 열렸습니다.");
            Close();
            return;
        }

        onSelected.Invoke(data);
        Close();
    }
}
