#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class WindowsAutoBuildVersion : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    private const string MenuRoot = "Tools/Build Version/";

    // EditorPrefs에 저장되므로 Unity를 껐다 켜도 유지됩니다.
    private const string SkipNextAutoPatchKey = "WindowsAutoBuildVersion.SkipNextAutoPatch";

    public void OnPreprocessBuild(BuildReport report)
    {
        if (!IsWindowsBuild(report.summary.platform))
        {
            return;
        }

        if (ShouldSkipNextAutoPatch())
        {
            SetSkipNextAutoPatch(false);
            Debug.Log($"[BuildVersion] Auto patch skipped once. Current version: {PlayerSettings.bundleVersion}");
            return;
        }

        IncreasePatchVersion(isManual: false);
    }

    [MenuItem(MenuRoot + "Increase Major Version")]
    private static void IncreaseMajorVersionMenu()
    {
        IncreaseMajorVersion();
        SetSkipNextAutoPatch(true);
    }

    [MenuItem(MenuRoot + "Increase Minor Version")]
    private static void IncreaseMinorVersionMenu()
    {
        IncreaseMinorVersion();
        SetSkipNextAutoPatch(true);
    }

    [MenuItem(MenuRoot + "Increase Patch Version")]
    private static void IncreasePatchVersionMenu()
    {
        IncreasePatchVersion(isManual: true);
        SetSkipNextAutoPatch(true);
    }

    [MenuItem(MenuRoot + "Print Current Version")]
    private static void PrintCurrentVersion()
    {
        Debug.Log($"[BuildVersion] Current version: {PlayerSettings.bundleVersion}");
    }

    [MenuItem(MenuRoot + "Enable Auto Patch For Next Build")]
    private static void EnableAutoPatchForNextBuild()
    {
        SetSkipNextAutoPatch(false);
        Debug.Log("[BuildVersion] Auto patch enabled for next Windows build.");
    }

    private static void IncreaseMajorVersion()
    {
        VersionData version = ParseVersion(PlayerSettings.bundleVersion);
        string oldVersion = version.ToString();

        version.major++;
        version.minor = 0;
        version.patch = 0;

        ApplyVersion(oldVersion, version.ToString(), "Major");
    }

    private static void IncreaseMinorVersion()
    {
        VersionData version = ParseVersion(PlayerSettings.bundleVersion);
        string oldVersion = version.ToString();

        version.minor++;
        version.patch = 0;

        ApplyVersion(oldVersion, version.ToString(), "Minor");
    }

    private static void IncreasePatchVersion(bool isManual)
    {
        VersionData version = ParseVersion(PlayerSettings.bundleVersion);
        string oldVersion = version.ToString();

        version.patch++;

        string increaseType = isManual ? "Manual patch" : "Auto patch";
        ApplyVersion(oldVersion, version.ToString(), increaseType);
    }

    private static void ApplyVersion(string oldVersion, string newVersion, string increaseType)
    {
        PlayerSettings.bundleVersion = newVersion;
        AssetDatabase.SaveAssets();

        Debug.Log($"[BuildVersion] {increaseType} version increased: {oldVersion} -> {newVersion}");
    }

    private static VersionData ParseVersion(string versionText)
    {
        if (string.IsNullOrWhiteSpace(versionText))
        {
            return new VersionData(1, 0, 0);
        }

        string[] parts = versionText.Split('.');

        int major = ParsePart(parts, 0, 1);
        int minor = ParsePart(parts, 1, 0);
        int patch = ParsePart(parts, 2, 0);

        return new VersionData(major, minor, patch);
    }

    private static int ParsePart(string[] parts, int index, int defaultValue)
    {
        if (parts.Length <= index)
        {
            return defaultValue;
        }

        return int.TryParse(parts[index], out int value) ? value : defaultValue;
    }

    private static bool IsWindowsBuild(BuildTarget target)
    {
        return target == BuildTarget.StandaloneWindows ||
               target == BuildTarget.StandaloneWindows64;
    }

    private static bool ShouldSkipNextAutoPatch()
    {
        return EditorPrefs.GetBool(SkipNextAutoPatchKey, false);
    }

    private static void SetSkipNextAutoPatch(bool value)
    {
        EditorPrefs.SetBool(SkipNextAutoPatchKey, value);
    }

    private struct VersionData
    {
        public int major;
        public int minor;
        public int patch;

        public VersionData(int major, int minor, int patch)
        {
            this.major = major;
            this.minor = minor;
            this.patch = patch;
        }

        public override string ToString()
        {
            return $"{major}.{minor}.{patch}";
        }
    }
}
#endif
