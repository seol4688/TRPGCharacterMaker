#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class BuildVersionInputWindow : EditorWindow
{
    private string versionText;

    [MenuItem("Tools/Build Version/Set Version Manually")]
    private static void Open()
    {
        BuildVersionInputWindow window = GetWindow<BuildVersionInputWindow>("Set Build Version");
        window.versionText = PlayerSettings.bundleVersion;
        window.minSize = new Vector2(300, 100);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Current Version", PlayerSettings.bundleVersion);

        EditorGUILayout.Space();

        versionText = EditorGUILayout.TextField("New Version", versionText);

        EditorGUILayout.HelpBox(
            "Format: 1.0.1 or 1.0.1f1",
            MessageType.Info
        );

        EditorGUILayout.Space();

        if (GUILayout.Button("Apply Version"))
        {
            ApplyVersion();
        }
    }

    private void ApplyVersion()
    {
        if (!IsValidVersion(versionText))
        {
            EditorUtility.DisplayDialog(
                "Invalid Version",
                "버전 형식이 올바르지 않습니다.\n예: 1.0.1 또는 1.0.1f1",
                "OK"
            );
            return;
        }

        string oldVersion = PlayerSettings.bundleVersion;

        PlayerSettings.bundleVersion = versionText;
        AssetDatabase.SaveAssets();

        Debug.Log($"[BuildVersion] Version manually changed: {oldVersion} -> {versionText}");

        Close();
    }

    private static bool IsValidVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return false;
        }

        string[] mainAndFix = version.Split('f');

        if (mainAndFix.Length > 2)
        {
            return false;
        }

        if (mainAndFix.Length == 2)
        {
            if (string.IsNullOrWhiteSpace(mainAndFix[1]))
            {
                return false;
            }

            if (!int.TryParse(mainAndFix[1], out int fix))
            {
                return false;
            }

            if (fix <= 0)
            {
                return false;
            }
        }

        string[] parts = mainAndFix[0].Split('.');

        if (parts.Length != 3)
        {
            return false;
        }

        for (int i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], out int value))
            {
                return false;
            }

            if (value < 0)
            {
                return false;
            }
        }

        return true;
    }
}
#endif
