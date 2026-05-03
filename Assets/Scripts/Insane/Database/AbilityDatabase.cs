using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AbilityDatabase", menuName = "Insane/Ability Database")]
public class AbilityDatabase : ScriptableObject
{
    [SerializeField] private List<AbilityData> abilities = new List<AbilityData>();

    public IReadOnlyList<AbilityData> Abilities => abilities;

    public AbilityData GetAbilityByName(string abilityName)
    {
        if (string.IsNullOrWhiteSpace(abilityName))
        {
            return null;
        }

        for (int i = 0; i < abilities.Count; i++)
        {
            if (abilities[i].name == abilityName)
            {
                return abilities[i];
            }
        }

        return null;
    }

    public List<AbilityData> GetAbilitiesByType(InsaneAbilityType type)
    {
        List<AbilityData> result = new List<AbilityData>();
        for (int i = 0; i < abilities.Count; i++)
        {
            if (abilities[i].type == type)
            {
                result.Add(abilities[i]);
            }
        }

        return result;
    }

    public List<AbilityData> GetAbilitiesByLibrary(string library)
    {
        List<AbilityData> result = new List<AbilityData>();
        for (int i = 0; i < abilities.Count; i++)
        {
            if (abilities[i].library == library)
            {
                result.Add(abilities[i]);
            }
        }

        return result;
    }

    public List<string> GetAllLibraries()
    {
        List<string> result = new List<string>();
        for (int i = 0; i < abilities.Count; i++)
        {
            string lib = abilities[i].library;
            if (!string.IsNullOrWhiteSpace(lib) && !result.Contains(lib))
            {
                result.Add(lib);
            }
        }

        result.Sort(System.StringComparer.OrdinalIgnoreCase);
        return result;
    }

#if UNITY_EDITOR
    /// <summary>기존 항목을 모두 교체합니다.</summary>
    public void SetAbilitiesFromImport(List<AbilityData> importedAbilities)
    {
        abilities = importedAbilities ?? new List<AbilityData>();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    /// <summary>
    /// 기존 항목을 유지하고, (이름 + 라이브러리)가 모두 중복되지 않는 항목만 뒤에 추가합니다.
    /// 이름이 같아도 라이브러리가 다르면 별개 항목으로 취급합니다.
    /// 반환값: 중복으로 건너뛴 수
    /// </summary>
    public int AppendAbilitiesFromImport(List<AbilityData> importedAbilities)
    {
        if (importedAbilities == null)
            return 0;

        System.Collections.Generic.HashSet<string> existingKeys =
            new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);

        for (int i = 0; i < abilities.Count; i++)
        {
            if (abilities[i] != null)
                existingKeys.Add(MakeKey(abilities[i].name, abilities[i].library));
        }

        int duplicateCount = 0;

        for (int i = 0; i < importedAbilities.Count; i++)
        {
            AbilityData data = importedAbilities[i];
            if (data == null)
                continue;

            string key = MakeKey(data.name, data.library);
            if (existingKeys.Contains(key))
            {
                duplicateCount++;
                continue;
            }

            abilities.Add(data);
            existingKeys.Add(key); // 같은 임포트 내 중복도 방지
        }

        UnityEditor.EditorUtility.SetDirty(this);
        return duplicateCount;
    }

    // "이름\0라이브러리" 형태의 복합 키 생성 (NUL 문자로 구분하여 충돌 방지)
    private static string MakeKey(string name, string library)
        => $"{name ?? string.Empty}\0{library ?? string.Empty}";
#endif
}
