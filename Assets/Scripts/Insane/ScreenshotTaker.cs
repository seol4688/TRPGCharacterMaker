using System.Collections;
using System.IO;
using Crosstales.FB;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 현재 화면을 PNG로 캡처하여 FileBrowser로 저장 위치를 선택합니다.
/// - 캡처 시 배경을 흰색(또는 지정 색)으로 임시 변경합니다.
/// - 파일명 기본값은 캐릭터 이름입니다.
/// </summary>
public class ScreenshotTaker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InsaneManager insaneManager;

    [Tooltip("배경색을 변경할 카메라. 비워두면 Camera.main을 사용합니다.")]
    [SerializeField] private Camera targetCamera;

    [Tooltip("(선택) 배경 UI Image가 있다면 연결합니다. 카메라와 함께 흰색으로 변경됩니다.")]
    [SerializeField] private Image backgroundImage;

    [Header("Screenshot Settings")]
    [Tooltip("캡처 시 배경에 적용할 색상 (기본: 흰색)")]
    [SerializeField] private Color screenshotBackgroundColor = Color.white;

    [Header("Crop Settings")]
    [Tooltip("중앙 기준 크롭 활성화")]
    [SerializeField] private bool enableCrop = true;
    [Tooltip("크롭 후 너비 (px)")]
    [SerializeField] private int cropWidth  = 1527;
    [Tooltip("크롭 후 높이 (px)")]
    [SerializeField] private int cropHeight = 1080;

    private static readonly ExtensionFilter[] PngFilter =
    {
        new ExtensionFilter("PNG 이미지", "png")
    };

    // ─── 외부 호출 ────────────────────────────────────────────

    /// <summary>
    /// 버튼 OnClick에서 호출합니다.
    /// 저장 경로를 먼저 확정한 뒤 캡처를 시작합니다 (취소 시 캡처 없음).
    /// </summary>
    public void TakeScreenshot()
    {
        if (FileBrowser.Instance == null)
        {
            Debug.LogWarning("[Screenshot] FileBrowser.Instance가 null — 바탕화면에 직접 저장합니다.");
            StartCoroutine(CaptureAndSaveTo(BuildFallbackPath()));
            return;
        }

        // ── 1. 먼저 저장 경로 확정 ─────────────────────────────
        FileBrowser.Instance.SaveFileAsync(
            path =>
            {
                if (string.IsNullOrEmpty(path))
                {
                    Debug.Log("[Screenshot] 저장 취소됨 — 캡처하지 않음");
                    return;
                }

                if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                    path += ".png";

                // ── 2. 경로가 확정됐을 때만 캡처 시작 ─────────
                StartCoroutine(CaptureAndSaveTo(path));
            },
            "스크린샷 저장",
            string.Empty,
            BuildDefaultFileName(),
            PngFilter);
    }

    // ─── 캡처 흐름 ────────────────────────────────────────────

    /// <summary>savePath가 확정된 뒤 호출됩니다. 배경 변경 → 캡처 → 복원 → 저장.</summary>
    private IEnumerator CaptureAndSaveTo(string savePath)
    {
        // ── 1. 배경을 흰색으로 임시 변경 ───────────────────────
        BackgroundState savedState = ApplyScreenshotBackground();

        // ── 2. 한 프레임 대기 → 변경된 배경 반영 후 캡처 ──────
        yield return new WaitForEndOfFrame();

        Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();

        // ── 3. 배경 즉시 복원 ──────────────────────────────────
        RestoreBackground(savedState);

        // ── 4. 중앙 크롭 ──────────────────────────────────────
        Texture2D finalTexture = screenshot;
        if (enableCrop)
        {
            Texture2D cropped = CropToCenter(screenshot, cropWidth, cropHeight);
            Destroy(screenshot);
            finalTexture = cropped;
        }

        // ── 5. PNG 인코딩 및 저장 ──────────────────────────────
        byte[] pngBytes = finalTexture.EncodeToPNG();
        Destroy(finalTexture);

        if (pngBytes == null || pngBytes.Length == 0)
        {
            Debug.LogWarning("[Screenshot] PNG 인코딩 실패");
            yield break;
        }

        try
        {
            File.WriteAllBytes(savePath, pngBytes);
            Debug.Log($"[Screenshot] 저장 완료: {savePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Screenshot] 저장 실패: {e.Message}");
        }
    }

    // ─── 배경 변경 / 복원 ────────────────────────────────────

    private struct BackgroundState
    {
        public Camera             cam;
        public CameraClearFlags   clearFlags;
        public Color              backgroundColor;
        public bool               camChanged;

        public Image              bgImage;
        public Color              imageColor;
        public bool               imageChanged;
    }

    private BackgroundState ApplyScreenshotBackground()
    {
        BackgroundState state = new BackgroundState();

        // 카메라 배경색 변경
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam != null)
        {
            state.cam             = cam;
            state.clearFlags      = cam.clearFlags;
            state.backgroundColor = cam.backgroundColor;
            state.camChanged      = true;

            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = screenshotBackgroundColor;
        }

        // UI 배경 Image 변경
        if (backgroundImage != null)
        {
            state.bgImage     = backgroundImage;
            state.imageColor  = backgroundImage.color;
            state.imageChanged = true;

            backgroundImage.color = screenshotBackgroundColor;
        }

        return state;
    }

    private void RestoreBackground(BackgroundState state)
    {
        if (state.camChanged && state.cam != null)
        {
            state.cam.clearFlags      = state.clearFlags;
            state.cam.backgroundColor = state.backgroundColor;
        }

        if (state.imageChanged && state.bgImage != null)
        {
            state.bgImage.color = state.imageColor;
        }
    }

    // ─── 크롭 ────────────────────────────────────────────────

    /// <summary>
    /// source 텍스처를 화면 중앙 기준으로 (targetW × targetH) 크기로 잘라냅니다.
    /// targetW/H가 source보다 크면 source 크기로 클램프합니다.
    /// </summary>
    private static Texture2D CropToCenter(Texture2D source, int targetW, int targetH)
    {
        int srcW = source.width;
        int srcH = source.height;

        // 실제 크롭 크기 (source를 벗어나지 않도록 클램프)
        int w = Mathf.Min(targetW, srcW);
        int h = Mathf.Min(targetH, srcH);

        // 중앙 기준 시작 좌표
        // Unity 텍스처 좌표계: (0,0) = 좌하단
        int x = (srcW - w) / 2;
        int y = (srcH - h) / 2;

        Color[] pixels = source.GetPixels(x, y, w, h);

        Texture2D cropped = new Texture2D(w, h, TextureFormat.RGBA32, false);
        cropped.SetPixels(pixels);
        cropped.Apply();

        return cropped;
    }

    // ─── 파일명 생성 ─────────────────────────────────────────

    private string BuildDefaultFileName()
    {
        string characterName = GetCharacterName();

        if (string.IsNullOrWhiteSpace(characterName))
            characterName = "screenshot";

        // 파일명에 사용할 수 없는 문자 제거
        foreach (char c in Path.GetInvalidFileNameChars())
            characterName = characterName.Replace(c, '_');

        return characterName;
    }

    private string GetCharacterName()
    {
        if (insaneManager == null)
            insaneManager = FindObjectOfType<InsaneManager>();

        return insaneManager?.CurrentSheet?.profile?.name ?? string.Empty;
    }

    // ─── 폴백 경로 생성 (FileBrowser 없을 때) ───────────────

    private string BuildFallbackPath()
    {
        string dir  = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        string name = BuildDefaultFileName();
        string path = Path.Combine(dir, name + ".png");

        // 같은 이름 파일이 있으면 번호 붙이기
        int index = 1;
        while (File.Exists(path))
        {
            path = Path.Combine(dir, $"{name}_{index}.png");
            index++;
        }

        return path;
    }
}
