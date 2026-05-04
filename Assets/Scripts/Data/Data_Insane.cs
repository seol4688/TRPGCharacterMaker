using System;
using System.Collections.Generic;
using UnityEngine;

// ─── 캐릭터 시트 ─────────────────────────────────────────────

[Serializable]
public class InsaneCharacterSheet
{
    public InsaneProfileData profile  = new InsaneProfileData();
    public int maxLife;
    public int maxSanity;
    public InsaneItemData     item     = new InsaneItemData();
    public InsaneSpecialtyData specialty = new InsaneSpecialtyData();
    public InsaneAbilityData  ability  = new InsaneAbilityData();
    public InsaneGrowthData   growth   = new InsaneGrowthData();
}

// ─── 성장 ─────────────────────────────────────────────────────

[Serializable]
public class InsaneGrowthData
{
    public const int MinFearLevel     = 1;
    public const int DefaultFearLevel = 1;

    /// <summary>누적 공적점.</summary>
    public int meritPoints  = 0;
    /// <summary>현재 공포심 (최솟값 MinFearLevel).</summary>
    public int fearLevel    = DefaultFearLevel;
    /// <summary>어빌리티 습득으로 증가된 선택 슬롯 수.</summary>
    public int abilityBonus = 0;
    /// <summary>경제력 상승으로 증가된 아이템 슬롯 수.</summary>
    public int itemBonus    = 0;
}

[Serializable]
public class InsaneProfileData
{
    public string avatarImagePath;
    public string name;
    public string gender;
    public string age;
    public string job;
}

// ─── 아이템 ──────────────────────────────────────────────────

[Serializable]
public enum InsaneItemType
{
    Painkiller,
    Weapon,
    Talisman
}

[Serializable]
public class InsaneItemData
{
    public List<string> startingItemNames = new List<string>();
    public List<InsaneItemStackData> items = new List<InsaneItemStackData>();
}

[Serializable]
public class InsaneItemStackData
{
    public string itemName;
    public int count;
}

// ─── 특성 ────────────────────────────────────────────────────

[Serializable]
public class InsaneSpecialtyData
{
    public string[] areaNames;
    public List<InsaneSpecialtyEntryData> specialties = new List<InsaneSpecialtyEntryData>();
    public string curiosityAreaName;
    public List<string> fearSpecialtyNames = new List<string>();
}

[Serializable]
public class InsaneSpecialtyEntryData
{
    public int column;
    public int row;
    public string specialtyName;
    public int difficulty;
    public bool isChecked;
}

[Serializable]
public class CustomSheetSaveData
{
    public string[] areaNames;
    public string[] skillNames;
    public bool isFavorite;
}

// ─── 어빌리티 (저장 데이터) ──────────────────────────────────

[Serializable]
public class InsaneAbilityData
{
    public List<string> acquiredAbilityNames = new List<string>();
    public List<InsaneAbilityEntryData> abilities = new List<InsaneAbilityEntryData>();
}

[Serializable]
public class InsaneAbilityEntryData
{
    public string abilityName;
    public string designatedSpecialtyName;
}

// ─── 어빌리티 (데이터베이스) ─────────────────────────────────

[Serializable]
public enum InsaneAbilityType
{
    Attack,    // 공격
    Support,   // 서포트
    Equipment  // 장비
}

[Serializable]
public enum DesignatedSpecialtyType
{
    Specific,     // 특정 특성 (여러 개 가능)
    AnyInArea,    // 분야 내 아무 특성
    Mixed,        // 특정 특성 + 분야 혼합
    None,         // 없음
    Variable,     // 가변
    AnySpecialty  // 분야 무관 전체 특성 중 아무거나 (예: 기본공격)
}

[Serializable]
public enum AcquireMultipleMode
{
    Disallowed, // 중복 획득 불가
    Unlimited,  // 무제한 중복 획득 가능
    Limited     // 최대 N개까지 중복 획득 가능
}

/// <summary>
/// 어빌리티 획득/해제 시 발동·취소되는 특수 이벤트.
/// 스탯 수치 변화가 아닌 기능 토글에 사용합니다.
/// </summary>
[Serializable]
public enum AbilitySpecialEffect
{
    None             = 0, // 특수 효과 없음
    WrapHorizontally = 1, // 특성 그리드 수평 순환 활성화 (이시키 근원법칙)
}

[Serializable]
public class DesignatedSpecialtyEntry
{
    public bool isAreaEntry; // false = 특정 특성, true = 분야 내 아무 특성
    public string value;     // 특성명 or 분야명
}

/// <summary>
/// 어빌리티 획득/해제 시 적용·취소되는 스탯 보너스.
/// </summary>
[Serializable]
public class AbilityStatEffect
{
    [Tooltip("최대 생명력 보너스 (음수 가능)")]
    public int lifeBonus      = 0;
    [Tooltip("최대 이성치 보너스 (음수 가능)")]
    public int sanityBonus    = 0;
    [Tooltip("선택 가능 특성 수 보너스 (음수 가능)")]
    public int specialtyBonus = 0;

    public bool IsEmpty => lifeBonus == 0 && sanityBonus == 0 && specialtyBonus == 0;
}

[Serializable]
public class AbilityData
{
    public string name;
    public InsaneAbilityType type;
    public DesignatedSpecialtyType designatedSpecialtyType;
    public List<DesignatedSpecialtyEntry> designatedEntries = new List<DesignatedSpecialtyEntry>();
    public string effect;
    public string library;
    public AcquireMultipleMode acquireMode = AcquireMultipleMode.Disallowed;
    public int maxAcquireCount = 1;                          // acquireMode == Limited 일 때만 사용
    public AbilityStatEffect statEffect = new AbilityStatEffect();
    public string acquireCondition;                          // 습득조건 (비어 있으면 조건 없음)
    public AbilitySpecialEffect specialEffect = AbilitySpecialEffect.None; // 특수 이벤트 (기본값: 없음)
}
