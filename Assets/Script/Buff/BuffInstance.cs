using System.Collections.Generic;
using UnityEngine;

public class BuffInstance
{
    public int BuffId { get; private set; }
    public CharacterBehaviour owner { get; private set; }
    public BuffData buffData;
    private float duration;
    private int stackCount; // 현재 버프의 스택 수
    private int maxStackCount; // 최대 스택 수

    private Dictionary<BuffTriggerType, int> triggerCounts = new Dictionary<BuffTriggerType, int>();
    private float fSecondTimer = 0f; // EveryFSecond용 타이머

    public bool IsExpired => duration <= 0;

    public BuffInstance(int id, BuffData data, CharacterBehaviour owner)
    {
        BuffId = id;
        buffData = data;
        this.owner = owner;
        duration = data.ReleaseData.Type == BuffReleaseType.Time ? data.ReleaseData.Time : float.MaxValue;
        
        stackCount = 1; // 기본 스택 수는 1
        maxStackCount = data.DuplicationData.MaxStackCount; // 설정된 최대 스택 수를 가져옴
    }

    public void Activate()
    {
        Debug.Log($"Buff {BuffId} activated on {owner.name}");
        TriggerEffect(BuffTriggerType.StartThisBuff);
    }

    // Buff 비활성화
    public void Deactivate()
    {
        Debug.Log($"Buff {BuffId} deactivated on {owner.name}");
        TriggerEffect(BuffTriggerType.EndThisBuff); // 버프 종료 시 트리거
        // Buff 해제 효과 적용
    }

    /// 버프의 스택 증가 로직 추가
    public void AddStack(int count)
    {
        if (stackCount < maxStackCount)
        {
            stackCount += count;
            TriggerEffect(BuffTriggerType.StackThisBuff); // 스택 증가 시 트리거
            Debug.Log($"Buff {BuffId} stack increased: {stackCount}/{maxStackCount}");

            // 스택이 특정 값에 도달하면 트리거 발동
            if (stackCount == buffData.DuplicationData.MaxStackCount)
            {
                TriggerEffect(BuffTriggerType.NStackofThisBuff); // 특정 스택에 도달 시 트리거
            }
        }
    }

    // 주기적으로 호출되는 업데이트 함수
    public void Update(float deltaTime)
    {
        duration -= deltaTime;
        // EveryFSecond 트리거 처리
        foreach (var unit in buffData.Units)
        {
            if (unit.Trigger.Type == BuffTriggerType.EveryFSecond)
            {
                fSecondTimer += deltaTime;
                if (fSecondTimer >= unit.Trigger.FValue)
                {
                    fSecondTimer = 0f;
                    TriggerEffect(BuffTriggerType.EveryFSecond);
                }
            }
        }
    }

    // 특정 트리거 타입을 확인하는 메서드 추가
    public bool CheckTrigger(BuffTriggerType triggerType, HitType hitType = HitType.DamageOnly)
    {
        foreach (var unit in buffData.Units)
        {
            if (unit.Trigger.Type == triggerType && (unit.Trigger.HitFilter.HitType == hitType || hitType == HitType.All))
                return true;
        }
        return false;
    }

    // BuffUnit의 조건 체크
    public bool CheckConditions(List<BuffConditionData> conditions)
    {
        foreach (var condition in conditions)
        {
            if (!CheckCondition(condition))
            {
                return false;
            }
        }
        return true;
    }

    // 단일 조건 검사 메서드
    private bool CheckCondition(BuffConditionData condition)
    {
        //뭐...이런식
        if (condition.Type == BuffConditionType.OwnerHpPercent)
        {
            float hpPercent = 0; // 캐릭터의 현재 체력 퍼센트 계산
            return CompareValue(hpPercent, condition.Comparator, condition.ComparativeValueFixed);
        }
        return true;
    }

    // 값을 비교하는 메서드
    private bool CompareValue(float value, Comparator comparator, float comparativeValue)
    {
        return comparator switch
        {
            Comparator.Equal => value == comparativeValue,
            Comparator.NotEqual => value != comparativeValue,
            Comparator.LEqual => value <= comparativeValue,
            Comparator.GEqual => value >= comparativeValue,
            Comparator.LessThan => value < comparativeValue,
            Comparator.GreaterThan => value > comparativeValue,
            _ => false,
        };
    }
    public void TriggerEffect(BuffTriggerType triggerType)
    {
        // 트리거 호출 횟수 증가
        if (!triggerCounts.ContainsKey(triggerType))
        {
            triggerCounts[triggerType] = 0;
        }
        triggerCounts[triggerType]++;

        foreach (var unit in buffData.Units)
        {
            if (unit.Trigger.Type == triggerType && CheckConditions(unit.Conditions))
            {
                if (triggerCounts[triggerType] >= unit.Trigger.EveryNTimes)
                {
                    // BuffUnit에 정의된 target을 기준으로 효과를 실행
                    triggerCounts[triggerType] = 0; // 트리거 횟수 초기화
                    var target = DetermineTarget(unit.Target);
                    ExecuteEffect(unit.Effects, target);
                }
            }
        }
    }

    private object DetermineTarget(BuffTargetData targetData)
    {
        // BuffTargetType에 따라 적절한 오브젝트를 찾는 로직을 추가
        switch (targetData.Type)
        {
            case BuffTargetType.Owner:
                return owner; // CharacterBehaviour 타입의 owner 반환

            case BuffTargetType.ThisBuff:
                return this; // 현재 BuffInstance 자신을 반환

            case BuffTargetType.Bullet:
                return GetRecentBullet(); // 가장 최근에 발생한 BulletBehaviour 객체 반환

            case BuffTargetType.Victim:
                return GetRecentVictim(); // 가장 최근에 피해를 입은 캐릭터 반환...이딴식으로하는게 맞나?

            case BuffTargetType.Attacker:
                return GetRecentAttacker(); // 가장 최근 공격한 캐릭터 반환

            // 필요한 다른 BuffTargetType에 대한 로직 추가
            default:
                return null;
        }
    }

    // Bullet과 Victim 등 최근 발생한 오브젝트를 가져오는 예시 메서드
    private BulletBehaviour GetRecentBullet()
    {
        // 최근에 생성된 Bullet을 관리하는 로직을 BuffManager 또는 BuffInstance에 구현
        //return BuffManager.Instance.GetLastSpawnedBulletForOwner(owner);
        return null;
    }

    private CharacterBehaviour GetRecentVictim()
    {
        //return BuffManager.Instance.GetLastVictimForOwner(owner);
        return null;
    }

    private CharacterBehaviour GetRecentAttacker()
    {
        //return BuffManager.Instance.GetLastAttackerForOwner(owner);
        return null;
    }

    // Buff 효과 발동
    public void ExecuteEffect(List<BuffEffectData> effects, object target)
    {
        CharacterBehaviour targetCharacter = target as CharacterBehaviour;
        BulletBehaviour targetBullet = target as BulletBehaviour;
        BuffInstance targetBuff = target as BuffInstance;
        
        foreach (var effect in effects)
        {
            switch (effect.Type)
            {
                case BuffEffectType.Character_SpawnBullet:

                    Vector3 spawnPosition = targetCharacter.transform.position + targetCharacter.transform.right * effect.Position.x + targetCharacter.transform.forward * effect.Position.y;
                    Vector3 spawnDirection = Quaternion.Euler(0, effect.Angle, 0) * targetCharacter.transform.forward;
                    BuffManager.Instance.SpawnBullet(effect.BulletPrefab, spawnPosition, spawnDirection, owner);
                    
                    break;

                case BuffEffectType.Character_Heal:
                    float healAmount = effect.Value;
                    // Heal 메서드를 통해 대상의 HP 회복
                    break;

                case BuffEffectType.Character_AddBuff:
                    BuffData newBuffData = effect.BuffData;
                    BuffManager.Instance.AddBuff(newBuffData, targetCharacter);
                    break;

                case BuffEffectType.Character_SetHp:
                    // victim.SetHealth(effect.Value);
                    break;

                case BuffEffectType.Character_InstantDamage:
                    targetCharacter.TakeDamage(effect.Value, Vector3.zero, HitType.DamageOnly, 0f, 0f, owner);
                    break;

                case BuffEffectType.Character_AddResource:
                    targetCharacter.resourceTable.AddResource(effect.CharacterResourceKey, (int)effect.Value);
                    break;
                case BuffEffectType.Character_SetResource:
                    targetCharacter.resourceTable.SetResource(effect.CharacterResourceKey, (int)effect.Value);
                    break;
                case BuffEffectType.Buff_ChangeStack:
                    targetBuff.AddStack((int)effect.Value);
                    break;

                default:
                    Debug.LogWarning($"Unknown BuffEffectType: {effect.Type}");
                    break;
            }
        }
    }
}
