#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class WindowsManualBuildVersion
{
    private const string MenuRoot = "Tools/Build Version/";

    [MenuItem(MenuRoot + "Increase Major Version")]
    private static void IncreaseMajorVersionMenu()
    {
        IncreaseMajorVersion();
    }

    [MenuItem(MenuRoot + "Increase Minor Version")]
    private static void IncreaseMinorVersionMenu()
    {
        IncreaseMinorVersion();
    }

    [MenuItem(MenuRoot + "Increase Patch Version")]
    private static void IncreasePatchVersionMenu()
    {
        IncreasePatchVersion();
    }

    [MenuItem(MenuRoot + "Increase Bugfix Version")]
    private static void IncreaseBugfixVersionMenu()
    {
        IncreaseBugfixVersion();
    }

    [MenuItem(MenuRoot + "Print Current Version")]
    private static void PrintCurrentVersion()
    {
        Debug.Log($"[BuildVersion] Current version: {PlayerSettings.bundleVersion}");
    }

    private static void IncreaseMajorVersion()
    {
        VersionData version = ParseVersion(PlayerSettings.bundleVersion);
        string oldVersion = version.ToString();

        version.major++;
        version.minor = 0;
        version.patch = 0;
        version.fix = 0;

        ApplyVersion(oldVersion, version.ToString(), "Major");
    }

    private static void IncreaseMinorVersion()
    {
        VersionData version = ParseVersion(PlayerSettings.bundleVersion);
        string oldVersion = version.ToString();

        version.minor++;
        version.patch = 0;
        version.fix = 0;

        ApplyVersion(oldVersion, version.ToString(), "Minor");
    }

    private static void IncreasePatchVersion()
    {
        VersionData version = ParseVersion(PlayerSettings.bundleVersion);
        string oldVersion = version.ToString();

        version.patch++;
        version.fix = 0;

        ApplyVersion(oldVersion, version.ToString(), "Patch");
    }

    private static void IncreaseBugfixVersion()
    {
        VersionData version = ParseVersion(PlayerSettings.bundleVersion);
        string oldVersion = version.ToString();

        version.fix++;

        ApplyVersion(oldVersion, version.ToString(), "Bugfix");
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
            return new VersionData(1, 0, 0, 0);
        }

        string[] mainAndFix = versionText.Split('f');

        string mainVersionText = mainAndFix[0];
        int fix = 0;

        if (mainAndFix.Length > 1)
        {
            int.TryParse(mainAndFix[1], out fix);
        }

        string[] parts = mainVersionText.Split('.');

        int major = ParsePart(parts, 0, 1);
        int minor = ParsePart(parts, 1, 0);
        int patch = ParsePart(parts, 2, 0);

        return new VersionData(major, minor, patch, fix);
    }

    private static int ParsePart(string[] parts, int index, int defaultValue)
    {
        if (parts.Length <= index)
        {
            return defaultValue;
        }

        return int.TryParse(parts[index], out int value) ? value : defaultValue;
    }

    private struct VersionData
    {
        public int major;
        public int minor;
        public int patch;

        // 0이면 f를 붙이지 않습니다.
        // 1 이상이면 1.0.1f1, 1.0.1f2 형태가 됩니다.
        public int fix;

        public VersionData(int major, int minor, int patch, int fix)
        {
            this.major = major;
            this.minor = minor;
            this.patch = patch;
            this.fix = fix;
        }

        public override string ToString()
        {
            string mainVersion = $"{major}.{minor}.{patch}";

            if (fix <= 0)
            {
                return mainVersion;
            }

            return $"{mainVersion}f{fix}";
        }
    }
}
#endif