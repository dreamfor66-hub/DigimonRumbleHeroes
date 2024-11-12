using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.Animations;
using System;

[CreateAssetMenu(fileName = "Data_Buff_New", menuName = "Data/Buff Data", order = 1)]
public class BuffData : ScriptableObject
{
    public List<BuffUnitData> Units = new();

    [HideLabel]
    [Title("해제조건")]
    public BuffReleaseData ReleaseData = new();

    [HideLabel]
    [Title("중복버프처리")]
    public BuffDuplicationData DuplicationData = new();
}

// Inner Classes


[Serializable]
public class BuffUnitData
{
    [HideLabel]
    [Title("조건")]
    public List<BuffConditionData> Conditions = new();
    [HideLabel]
    [Title("트리거")]
    public BuffTriggerData Trigger = new();
    [HideLabel]
    [Title("타겟")]
    public BuffTargetData Target = new();
    [HideLabel]
    [Title("효과")]
    public List<BuffEffectData> Effects = new();
}

public enum BuffConditionType
{
    Always,

    OwnerState = 11,
    OwnerHpPercent = 31,
    OwnerResource = 41,

    MonsterInStage = 100,
    MonsterAroundNMeter = 101,
}

public enum Comparator
{
    Equal,
    NotEqual,
    LEqual,
    GEqual,
    LessThan,
    GreaterThan,
}

[Serializable]
public class BuffConditionData
{
    public BuffConditionType Type;

    [ShowIf(nameof(Type), BuffConditionType.OwnerResource)]
    public CharacterResourceKey ResourceKey;

    [ShowIf(nameof(ShowDistance))]
    public float Distance;
    [ShowIf(nameof(ShowComparator))]
    public Comparator Comparator;
    [ShowIf(nameof(ShowComparativeValueInt))]
    public int ComparativeValueInt;

    [ShowIf(nameof(ShowComparativeValueFixed))]
    public float ComparativeValueFixed;

    [ShowIf(nameof(ShowStateFilter))]
    public CharacterState State;

    private bool ShowStateFilter => Type is BuffConditionType.OwnerState;
    private bool ShowDistance => Type is BuffConditionType.MonsterAroundNMeter;
    private bool ShowComparator => Type is BuffConditionType.MonsterAroundNMeter or BuffConditionType.MonsterInStage or BuffConditionType.OwnerHpPercent or BuffConditionType.OwnerResource;

    private bool ShowComparativeValueInt =>
        Type is BuffConditionType.MonsterInStage or BuffConditionType.MonsterAroundNMeter or BuffConditionType.OwnerResource;

    private bool ShowComparativeValueFixed =>
        Type is BuffConditionType.OwnerHpPercent;
}

public enum BuffTriggerType
{
    DuringThisBuff = 0,
    StartThisBuff = 10,
    EndThisBuff = 11,
    StackThisBuff = 12,
    NStackofThisBuff = 13,
    //StageStart = 20,
    //StageClear = 21,
    EveryFSecond = 30,
    EveryFMeterMove = 40,

    OwnerAction = 100,
    OwnerHit = 110,
    OwnerHurt = 120,
    OwnerDie = 140,
    OwnerSpawnBullet = 150,

    EnemyDead = 200,

    EveryHit = 300,
}

[Flags]
public enum MoveCauseType
{
    Run = 1 << 0,
    Action = 1 << 1,
    KnockBack = 1 << 2,
    All = ~0,
}

public enum HitFilterType
{
    Always,

    HitType = 10,

    ResultType = 20,

    TriggerTeam = 30,

    IsSmash = 100,
    IsNotSmash = 101,

    OwnerToVictimDistance = 110,
    IncludeOwner = 120,
}

[HideLabel]
[Serializable]
public class HitFilter
{
    public List<HitFilterUnit> units = new();
}

[Serializable]
public class HitFilterUnit
{
    public HitFilterType Type;
    public HitType HitType = HitType.All;
    //public TeamType TriggerTeam = TeamType.All;
}


[Serializable]
public class BuffTriggerData
{
    public BuffTriggerType Type;

    [ShowIf(nameof(ShowFValue))] public float FValue;
    [ShowIf(nameof(ShowStackFilter))] public int NValue = new();
    [ShowIf(nameof(ShowMoveFilter))] public MoveCauseType MoveFilter = MoveCauseType.All;
    [ShowIf(nameof(ShowHitFilter))] public HitFilter HitFilter = new();

    public bool ShowFValue => Type is BuffTriggerType.EveryFSecond or BuffTriggerType.EveryFMeterMove;
    public bool ShowMoveFilter => Type is BuffTriggerType.EveryFMeterMove;
    public bool ShowHitFilter => Type is BuffTriggerType.OwnerHit or BuffTriggerType.OwnerHurt or BuffTriggerType.EveryHit;
    public bool ShowStackFilter => Type is BuffTriggerType.NStackofThisBuff;

    [HideIf(nameof(Type), BuffTriggerType.DuringThisBuff)]
    public int EveryNTimes = 1;
}


public enum BuffReleaseType
{
    Permanent = 0,
    Instant = 1,
    Time = 10,
    TriggerCount = 20,
}

[Serializable]
public class BuffReleaseData
{
    public BuffReleaseType Type;
    [ShowIf(nameof(Type), BuffReleaseType.Time)] public float Time;
    [ShowIf(nameof(ShowCount))] public int Count;

    public bool ShowCount => Type is BuffReleaseType.TriggerCount;
}

public enum BuffTargetType
{
    OwnerStat = 0,

    Owner = 5,
    ThisBuff = 6,

    Action = 10,
    Hit = 11,
    Bullet = 14,

    Victim = 21,
    Attacker = 22,
}

[Serializable]
public class BuffTargetData
{
    public BuffTargetType Type;

}

public enum BuffEffectType
    {
        CharacterStat = 200,

        Character_Heal = 300,
        Character_HealPercent = 301,
        Character_AddBuff = 305,
        Character_RemoveBuff = 310,
        Character_SpawnBullet = 320,
        Character_InstantDamage = 325,
        Character_SetHp = 335,
        Character_AddResource = 360,
        Character_SetResource = 361,

        Action_AttackPowerPercent = 400,
        Action_AttackScalePercent,
        Action_KnockBackPowerPercent,
        Action_ActionSpeedPercent,
        Action_MoveAmountPercent,

        Hit_AttackPowerPercent = 500,
        Hit_DamageZero,
        Hit_CriticalDamagePercent = 520,

        Hit_KnockBackGrade = 540,
        Hit_KnockBackIgnore = 541,
        Hit_KnockBackGradeFixed = 542,
        Hit_KnockBackCorrection = 550,


        Bullet_AttackPowerPercent = 600,
        Bullet_FinalDamagePercent = 601,
        Bullet_PierceCount = 611,
        Bullet_SpeedPercent = 620,
        Bullet_KnockBackPower = 640,
        Bullet_HitApplyBuff = 650,
        Bullet_ScalePercent = 660,

        Buff_RemoveBuff = 700,
        Buff_ChangeStack = 710,

        Common_TriggerVfx = 800,
    }

    public enum TeamSelector
    {
        Target,
        BuffOwner,
    }

    public enum BuffValueVariableType
    {
        None,
        StackCount = 10,
        BuffTagStackCount = 11,
        BuffTagStack_X_PlayerATK = 12,
        OwnerHpPercent = 20,
        OwnerHpPercentReverse = 21,
        OwnerAttackPower = 30,
        PlayerAttackPower = 31,
    }

    public enum ResourceChangeType
    {
        Value,
        Percent,
    }

    [Serializable]
    public class BuffValueVariable
    {
        public BuffValueVariableType Type;
        public float Multiplier;
        public float Limit;
    }

[Serializable]
public class BuffEffectData
{
    public BuffEffectType Type;
    public float Value;
    public List<BuffValueVariable> ValueVariables = new();
    public BuffData BuffData;
    public BulletBehaviour BulletPrefab;
    public Vector2 Position;
    public float Angle;
    public TeamSelector teamSelector;
    public CharacterResourceKey CharacterResourceKey;
    public ResourceChangeType ResourceChangeType;


}
public enum BuffDuplicationType
{
    [LabelText("별개 버프로 취급")] None,
    [LabelText("기존 버프 유지, 새 버프 무시")] UsePrev,
    [LabelText("기존 버프 삭제, 새 버프 추가")] UseNew,
    [LabelText("스택 카운트")] StackCount,
}

[Serializable]
public class BuffDuplicationData
{
    public BuffDuplicationType Type;
    [ShowIf(nameof(Type), BuffDuplicationType.StackCount)] public int MaxStackCount;
}