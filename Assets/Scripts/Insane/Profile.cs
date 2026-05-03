using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Profile : MonoBehaviour
{
    private InsaneManager insaneManager;

    [Header("아바타 이미지")]
    [SerializeField] private RawImage profileRawImage;
    private AspectRatioFitter profileAspectRatioFitter;
    private static readonly AspectRatioFitter.AspectMode ProfileImageAspectMode = AspectRatioFitter.AspectMode.FitInParent;

    private Texture2D loadedProfileTexture;
    private bool loadedProfileTextureShouldDestroy;

    [Header("CharacterInfo")]
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private TMP_InputField genderInputField;
    [SerializeField] private TMP_InputField ageInputField;
    [SerializeField] private TMP_InputField jobInputField;

    [Header("Stat")]
    [SerializeField] private TextMeshProUGUI lifeText;
    [SerializeField] private TextMeshProUGUI sanityText;
    [SerializeField] private TextMeshProUGUI meritPointText;

    public void SetInsaneManager(InsaneManager manager)
    {
        if (manager != null)
            insaneManager = manager;
    }

    private void OnEnable()
    {
        if (nameInputField != null)
            nameInputField.onValueChanged.AddListener(SetCharacterName);

        if (genderInputField != null)
            genderInputField.onValueChanged.AddListener(SetCharacterGender);

        if (ageInputField != null)
            ageInputField.onValueChanged.AddListener(SetCharacterAge);

        if (jobInputField != null)
            jobInputField.onValueChanged.AddListener(SetCharacterJob);
    }

    private void OnDisable()
    {
        if (nameInputField != null)
            nameInputField.onValueChanged.RemoveListener(SetCharacterName);

        if (genderInputField != null)
            genderInputField.onValueChanged.RemoveListener(SetCharacterGender);

        if (ageInputField != null)
            ageInputField.onValueChanged.RemoveListener(SetCharacterAge);

        if (jobInputField != null)
            jobInputField.onValueChanged.RemoveListener(SetCharacterJob);
    }

    // ─── CharacterInfo ──────────────────────────────────────────

    public void SetCharacterName(string characterName)
    {
        if (insaneManager == null) return;
        insaneManager.EnsureCurrentSheet();
        insaneManager.CurrentSheet.profile.name = characterName ?? string.Empty;
    }

    public void SetCharacterGender(string characterGender)
    {
        if (insaneManager == null) return;
        insaneManager.EnsureCurrentSheet();
        insaneManager.CurrentSheet.profile.gender = characterGender ?? string.Empty;
    }

    public void SetCharacterAge(string characterAge)
    {
        if (insaneManager == null) return;
        insaneManager.EnsureCurrentSheet();
        insaneManager.CurrentSheet.profile.age = characterAge ?? string.Empty;
    }

    public void SetCharacterJob(string characterJob)
    {
        if (insaneManager == null) return;
        insaneManager.EnsureCurrentSheet();
        insaneManager.CurrentSheet.profile.job = characterJob ?? string.Empty;
    }

    // ─── Stat Display ────────────────────────────────────────────

    public void UpdateStatDisplay(int life, int sanity, int meritPoint)
    {
        if (lifeText != null)
            lifeText.text = $"{life}";

        if (sanityText != null)
            sanityText.text = $"{sanity}";

        if (meritPointText != null)
        {
            bool hasMerit = meritPoint > 0;
            meritPointText.gameObject.SetActive(hasMerit);
            if (hasMerit)
                meritPointText.text = $"{meritPoint}";
        }
    }

    // ─── Profile UI ──────────────────────────────────────────────

    /// <summary>시트 데이터를 UI에 반영합니다 (로드/리셋 시 호출).</summary>
    public void RefreshProfileUI(InsaneCharacterSheet sheet)
    {
        if (sheet == null) return;

        SetInputFieldText(nameInputField,   sheet.profile.name);
        SetInputFieldText(genderInputField, sheet.profile.gender);
        SetInputFieldText(ageInputField,    sheet.profile.age);
        SetInputFieldText(jobInputField,    sheet.profile.job);
        LoadProfileImageFromPath(sheet.profile.avatarImagePath);
    }

    /// <summary>현재 InputField 값을 시트에 동기화합니다 (저장 직전 호출).</summary>
    public void SyncProfileFromInputFields(InsaneCharacterSheet sheet)
    {
        if (sheet == null) return;

        if (nameInputField != null)
            sheet.profile.name   = nameInputField.text   ?? string.Empty;

        if (genderInputField != null)
            sheet.profile.gender = genderInputField.text ?? string.Empty;

        if (ageInputField != null)
            sheet.profile.age    = ageInputField.text    ?? string.Empty;

        if (jobInputField != null)
            sheet.profile.job    = jobInputField.text    ?? string.Empty;
    }

    // ─── Avatar Image ────────────────────────────────────────────

    public void OpenProfileImageFile()
    {
        FileBrowserManager.OpenImageFile(ApplyProfileImage);
    }

    public void LoadProfileImageFromPath(string imagePath)
    {
        if (profileRawImage == null)
        {
            return;
        }

        FileBrowserManager.ImageLoadResult imageLoadResult = FileBrowserManager.LoadImageTextureOrDefault(imagePath);
        insaneManager.CurrentSheet.profile.avatarImagePath = imageLoadResult.loadedFromPath ? imagePath : string.Empty;
        SetProfileRawImageTexture(imageLoadResult.texture, imageLoadResult.shouldDestroyTexture);
    }

    private void ApplyProfileImage(string imagePath, Texture2D texture)
    {
        if (texture == null)
        {
            return;
        }

        insaneManager.EnsureCurrentSheet();
        insaneManager.CurrentSheet.profile.avatarImagePath = imagePath ?? string.Empty;
        SetProfileRawImageTexture(texture, true);
    }

    private void SetProfileRawImageTexture(Texture2D texture, bool shouldDestroyWhenReplaced)
    {
        Texture2D previousTexture = loadedProfileTexture;
        bool shouldDestroyPreviousTexture = loadedProfileTextureShouldDestroy;

        loadedProfileTexture = texture;
        loadedProfileTextureShouldDestroy = shouldDestroyWhenReplaced;

        if (profileRawImage != null)
        {
            FileBrowserManager.ApplyTextureToRawImage(
                profileRawImage,
                texture,
                ref profileAspectRatioFitter,
                true,
                ProfileImageAspectMode);
        }

        FileBrowserManager.DestroyTextureIfNeeded(previousTexture, shouldDestroyPreviousTexture && previousTexture != texture);
    }

    public void ClearLoadedProfileTexture()
    {
        if (loadedProfileTexture == null)
        {
            return;
        }

        if (profileRawImage != null && profileRawImage.texture == loadedProfileTexture)
        {
            profileRawImage.texture = null;
        }

        FileBrowserManager.DestroyTextureIfNeeded(loadedProfileTexture, loadedProfileTextureShouldDestroy);
        loadedProfileTexture = null;
        loadedProfileTextureShouldDestroy = false;
    }

    // ─── Helpers ─────────────────────────────────────────────────

    private static void SetInputFieldText(TMP_InputField inputField, string value)
    {
        if (inputField != null)
            inputField.SetTextWithoutNotify(value ?? string.Empty);
    }
}
