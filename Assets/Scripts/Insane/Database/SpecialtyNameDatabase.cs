using UnityEngine;

[CreateAssetMenu(fileName = "SpecialtyDatabase", menuName = "Insane/Specialty Database")]
public class SpecialtyNameDatabase : ScriptableObject
{
    public const int ColumnCount = 6;
    public const int RowCount = 11;
    public const int SkillItemCount = ColumnCount * RowCount;

    [SerializeField] private string[] areaNames = new string[ColumnCount];
    [SerializeField] private string[] defaultSkillNames = new string[SkillItemCount];

    public string GetAreaName(int index)
    {
        if (areaNames == null || index < 0 || index >= areaNames.Length)
        {
            return string.Empty;
        }

        return areaNames[index] ?? string.Empty;
    }

    public string[] GetAreaNames()
    {
        string[] result = new string[ColumnCount];
        for (int i = 0; i < ColumnCount; i++)
        {
            result[i] = GetAreaName(i);
        }

        return result;
    }

    public string GetSkillName(int index)
    {
        if (defaultSkillNames == null || index < 0 || index >= defaultSkillNames.Length)
        {
            return string.Empty;
        }

        return defaultSkillNames[index] ?? string.Empty;
    }

    public void ResetToBuiltInDefaults()
    {
        EnsureSize();

        for (int i = 0; i < BuiltInDefaultAreaNames.Length; i++)
        {
            areaNames[i] = BuiltInDefaultAreaNames[i];
        }

        for (int i = 0; i < BuiltInDefaultSkillNames.Length; i++)
        {
            defaultSkillNames[i] = BuiltInDefaultSkillNames[i];
        }
    }

    private void Reset()
    {
        ResetToBuiltInDefaults();
    }

    private void OnValidate()
    {
        EnsureSize();
    }

    private void EnsureSize()
    {
        if (areaNames == null || areaNames.Length != ColumnCount)
        {
            string[] resized = new string[ColumnCount];
            if (areaNames != null)
            {
                int copyCount = Mathf.Min(areaNames.Length, resized.Length);
                for (int i = 0; i < copyCount; i++)
                {
                    resized[i] = areaNames[i];
                }
            }

            areaNames = resized;
        }

        if (defaultSkillNames == null || defaultSkillNames.Length != SkillItemCount)
        {
            string[] resized = new string[SkillItemCount];
            if (defaultSkillNames != null)
            {
                int copyCount = Mathf.Min(defaultSkillNames.Length, resized.Length);
                for (int i = 0; i < copyCount; i++)
                {
                    resized[i] = defaultSkillNames[i];
                }
            }

            defaultSkillNames = resized;
        }
    }

    private static readonly string[] BuiltInDefaultAreaNames =
    {
        "폭력", "정서", "지각", "기술", "지식", "괴이"
    };

    private static readonly string[] BuiltInDefaultSkillNames =
    {
        "소각", "고문", "포박", "협박", "파괴", "구타", "절단", "찌르기", "사격", "전쟁", "매장",
        "연심", "기쁨", "걱정", "부끄러움", "웃음", "인내", "놀람", "노여움", "원한", "슬픔", "친애",
        "고통", "관능", "촉감", "냄새", "맛", "소리", "풍경", "추적", "예술", "제육감", "그늘",
        "분해", "전자기기", "정리", "약품", "효율", "미디어", "카메라", "탈것", "기계", "함정", "병기",
        "물리학", "수학", "화학", "생물학", "의학", "교양", "인류학", "역사", "민속학", "고고학", "천문학",
        "시간", "혼돈", "심해", "죽음", "영혼", "마술", "암흑", "종말", "꿈", "지저", "우주"
    };
}
