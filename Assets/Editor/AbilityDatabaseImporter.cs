using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class AbilityDatabaseImporter : EditorWindow
{
    private AbilityDatabase targetDatabase;
    private string filePath   = string.Empty;
    private string importLog  = string.Empty;
    private bool   hasError   = false;
    private int    importMode = 0; // 0 = 덮어쓰기, 1 = 추가

    private static readonly string[] ImportModeLabels = { "덮어쓰기", "추가 (중복 건너뜀)" };

    private const string AreaSuffix = " 분야에서 아무거나";
    private const string WindowTitle = "어빌리티 데이터베이스 임포터";

    [MenuItem("Tools/Insane/Ability Database Importer")]
    private static void OpenWindow()
    {
        GetWindow<AbilityDatabaseImporter>(WindowTitle);
    }

    private void OnGUI()
    {
        GUILayout.Label(WindowTitle, EditorStyles.boldLabel);
        GUILayout.Space(8);

        targetDatabase = (AbilityDatabase)EditorGUILayout.ObjectField(
            "대상 데이터베이스", targetDatabase, typeof(AbilityDatabase), false);

        // 현재 등록 수 표시
        if (targetDatabase != null)
        {
            EditorGUILayout.LabelField("현재 등록 수", $"{targetDatabase.Abilities.Count}개",
                EditorStyles.miniLabel);
        }

        GUILayout.Space(4);

        GUILayout.BeginHorizontal();
        filePath = EditorGUILayout.TextField("파일 경로 (.csv / .tsv / .txt)", filePath);
        if (GUILayout.Button("찾아보기", GUILayout.Width(70)))
        {
            string path = EditorUtility.OpenFilePanel("파일 선택", Application.dataPath, "csv,tsv,txt");
            if (!string.IsNullOrEmpty(path))
            {
                filePath = path;
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(8);

        // 임포트 방식 선택
        GUILayout.Label("임포트 방식", EditorStyles.boldLabel);
        importMode = GUILayout.Toolbar(importMode, ImportModeLabels);
        if (importMode == 0)
            EditorGUILayout.HelpBox("기존 데이터를 모두 지우고 파일 내용으로 교체합니다.", MessageType.Warning);
        else
            EditorGUILayout.HelpBox("기존 데이터를 유지하고 이름이 겹치지 않는 항목만 뒤에 추가합니다.", MessageType.Info);

        GUILayout.Space(8);
        DrawFormatGuide();
        GUILayout.Space(12);

        GUI.enabled = targetDatabase != null && !string.IsNullOrEmpty(filePath);
        if (GUILayout.Button("임포트", GUILayout.Height(30)))
        {
            Import();
        }
        GUI.enabled = true;

        if (!string.IsNullOrEmpty(importLog))
        {
            GUILayout.Space(8);
            EditorGUILayout.HelpBox(importLog, hasError ? MessageType.Error : MessageType.Info);
        }
    }

    private void DrawFormatGuide()
    {
        GUILayout.Label("파일 형식 (첫 줄은 헤더로 건너뜀)", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "열 순서: 라이브러리 [구분자] 이름 [구분자] 타입 [구분자] 지정특기 [구분자] 효과 [구분자] 습득조건\n" +
            "※ 습득조건 열은 생략 가능 — 없으면 빈 값으로 처리됩니다.\n\n" +
            "구분자: .csv → 쉼표  |  .tsv / .txt → 탭\n\n" +
            "타입: 트릭(공격) / 서포트 / 장비  (빈 셀은 위 행 값 유지)\n" +
            "라이브러리: 빈 셀은 위 행 값 유지\n\n" +
            "지정특기 형식 (자연어):\n" +
            "  없음\n" +
            "  가변\n" +
            "  전체          ← 분야 무관 66개 전체 중 하나\n" +
            "  폭력 분야에서 아무거나\n" +
            "  사격\n" +
            "  폭력 분야에서 아무거나, 병기\n\n" +
            "Excel 내보내기:\n" +
            "  TSV → 다른 이름으로 저장 → '텍스트(탭으로 분리)(*.txt)'\n" +
            "  CSV → 다른 이름으로 저장 → 'CSV UTF-8(쉼표로 분리)(*.csv)'",
            MessageType.None);
    }

    // ─── 임포트 ───────────────────────────────────────────────

    private bool IsCsv(string path)
    {
        return Path.GetExtension(path).Equals(".csv", StringComparison.OrdinalIgnoreCase);
    }

    private void Import()
    {
        if (!File.Exists(filePath))
        {
            SetLog("오류: 파일을 찾을 수 없습니다.", true);
            return;
        }

        try
        {
            bool csv = IsCsv(filePath);
            string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
            List<AbilityData> abilities = new List<AbilityData>();
            int successCount = 0;
            int skipCount = 0;

            string prevLibrary = string.Empty;
            string prevType = string.Empty;

            // 첫 줄(헤더) 건너뜀
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string[] cols = csv ? SplitCsvLine(line) : line.Split('\t');
                TrimAll(cols);

                if (cols.Length < 5)
                {
                    Debug.LogWarning($"[AbilityDatabaseImporter] {i + 1}번 줄: 컬럼 수 부족 ({cols.Length} / 5) — 건너뜀");
                    skipCount++;
                    continue;
                }

                // 빈 셀은 이전 행 값 유지 (병합셀 처리)
                string library = string.IsNullOrWhiteSpace(cols[0]) ? prevLibrary : cols[0];
                string typRaw  = string.IsNullOrWhiteSpace(cols[2]) ? prevType    : cols[2];
                prevLibrary = library;
                prevType    = typRaw;

                string name = cols[1];
                if (string.IsNullOrWhiteSpace(name))
                {
                    skipCount++;
                    continue;
                }

                if (!TryParseType(typRaw, out InsaneAbilityType type))
                {
                    Debug.LogWarning($"[AbilityDatabaseImporter] {i + 1}번 줄 '{name}': 알 수 없는 타입 '{typRaw}' — 건너뜀");
                    skipCount++;
                    continue;
                }

                ParseDesignatedSpecialty(cols[3],
                    out DesignatedSpecialtyType specialtyType,
                    out List<DesignatedSpecialtyEntry> entries);

                abilities.Add(new AbilityData
                {
                    name    = name,
                    type    = type,
                    designatedSpecialtyType = specialtyType,
                    designatedEntries       = entries,
                    effect           = cols[4],
                    library          = library,
                    acquireCondition = cols.Length > 5 ? cols[5] : string.Empty
                });
                successCount++;
            }

            string separator = csv ? "CSV(쉼표)" : "TSV(탭)";

            if (importMode == 0)
            {
                // 덮어쓰기
                targetDatabase.SetAbilitiesFromImport(abilities);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                SetLog($"임포트 완료 [{separator}] 덮어쓰기 — {successCount}개 등록, {skipCount}개 건너뜀", false);
            }
            else
            {
                // 추가
                int duplicateCount = targetDatabase.AppendAbilitiesFromImport(abilities);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                int addedCount = successCount - duplicateCount;
                SetLog($"임포트 완료 [{separator}] 추가 — {addedCount}개 추가, {duplicateCount}개 중복(이름+라이브러리) 건너뜀, {skipCount}개 파싱 오류", false);
            }
        }
        catch (Exception e)
        {
            SetLog($"오류: {e.Message}", true);
        }
    }

    // ─── 지정특기 파서 (자연어) ───────────────────────────────

    private void ParseDesignatedSpecialty(string text,
        out DesignatedSpecialtyType type,
        out List<DesignatedSpecialtyEntry> entries)
    {
        entries = new List<DesignatedSpecialtyEntry>();
        text = text.Trim();

        if (string.IsNullOrWhiteSpace(text) || text == "없음")
        {
            type = DesignatedSpecialtyType.None;
            return;
        }

        if (text == "가변")
        {
            type = DesignatedSpecialtyType.Variable;
            return;
        }

        if (text == "전체")
        {
            type = DesignatedSpecialtyType.AnySpecialty;
            return;
        }

        // 쉼표로 항목 분리
        string[] parts = text.Split(',');
        bool hasArea     = false;
        bool hasSpecific = false;

        for (int i = 0; i < parts.Length; i++)
        {
            string part = parts[i].Trim();
            if (string.IsNullOrWhiteSpace(part))
            {
                continue;
            }

            if (part.EndsWith(AreaSuffix))
            {
                string areaName = part.Substring(0, part.Length - AreaSuffix.Length).Trim();
                entries.Add(new DesignatedSpecialtyEntry { isAreaEntry = true, value = areaName });
                hasArea = true;
            }
            else
            {
                entries.Add(new DesignatedSpecialtyEntry { isAreaEntry = false, value = part });
                hasSpecific = true;
            }
        }

        if (hasArea && hasSpecific) type = DesignatedSpecialtyType.Mixed;
        else if (hasArea)           type = DesignatedSpecialtyType.AnyInArea;
        else if (hasSpecific)       type = DesignatedSpecialtyType.Specific;
        else                        type = DesignatedSpecialtyType.None;
    }

    // ─── 타입 파서 ───────────────────────────────────────────

    private bool TryParseType(string text, out InsaneAbilityType type)
    {
        switch (text)
        {
            case "트릭":
            case "공격":   type = InsaneAbilityType.Attack;    return true;
            case "서포트": type = InsaneAbilityType.Support;   return true;
            case "장비":   type = InsaneAbilityType.Equipment; return true;
            default:       type = InsaneAbilityType.Attack;    return false;
        }
    }

    // ─── CSV 파서 (따옴표 처리) ───────────────────────────────

    private string[] SplitCsvLine(string line)
    {
        List<string> fields = new List<string>();
        StringBuilder current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
        }

        fields.Add(current.ToString());
        return fields.ToArray();
    }

    // ─── 유틸 ─────────────────────────────────────────────────

    private void TrimAll(string[] cols)
    {
        for (int i = 0; i < cols.Length; i++)
        {
            cols[i] = cols[i].Trim();
        }
    }

    private void SetLog(string message, bool error)
    {
        importLog = message;
        hasError = error;
        Repaint();
    }
}
