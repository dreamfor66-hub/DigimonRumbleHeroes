using System.Collections.Generic;
using System.Linq;
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

    private List<BuffEffectData> activeEffects;


    public BuffInstance(int id, BuffData data, CharacterBehaviour owner)
    {
        BuffId = id;
        this.buffData = data;
        this.owner = owner;
        duration = data.ReleaseData.Type == BuffReleaseType.Time ? data.ReleaseData.Time : float.MaxValue;
        
        stackCount = 1; // 기본 스택 수는 1
        maxStackCount = data.DuplicationData.MaxStackCount; // 설정된 최대 스택 수를 가져옴

        activeEffects = new List<BuffEffectData>();
        // BuffData의 모든 BuffUnitData에서 효과를 수집하여 activeEffects에 추가
        foreach (var unit in buffData.Units)
        {
            activeEffects.AddRange(unit.Effects);
        }
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
            if (unit.Trigger.Type == triggerType)
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

    public void TriggerEffect(BuffTriggerType triggerType, object parameter = null)
    {
        if (triggerType == BuffTriggerType.OwnerHit && parameter is HitData hitData)
        {
            if (hitData.Attacker != owner)
            {
                return; // Attacker가 Owner가 아니므로 실행 종료
            }

            if (!IsValidHit(hitData))
            {
                return; // HitType이 일치하지 않으면 종료
            }
        }

        if (!triggerCounts.ContainsKey(triggerType))
        {
            triggerCounts[triggerType] = 0;
        }
        triggerCounts[triggerType]++;

        foreach (var unit in buffData.Units)
        {
            if (unit.Trigger.Type == triggerType && CheckConditions(unit.Conditions))
            {
                triggerCounts[triggerType] = 0; // 트리거 횟수 초기화
                var target = DetermineTarget(unit.Target, parameter);
                // 필요한 매개변수를 `parameters` 배열에서 추출하여 사용
                ExecuteEffect(unit.Effects, target);
            }
        }
    }

    private object DetermineTarget(BuffTargetData targetData, object parameter)
    {
        HitData hit = parameter as HitData;
        // BuffTargetType에 따라 적절한 오브젝트를 찾는 로직을 추가
        switch (targetData.Type)
        {
            case BuffTargetType.Owner:
                return owner; // CharacterBehaviour 타입의 owner 반환

            case BuffTargetType.ThisBuff:
                return this; // 현재 BuffInstance 자신을 반환

            case BuffTargetType.Bullet:
                return parameter; 

            case BuffTargetType.Victim:
                if (hit.Victim != null)
                    return hit.Victim; // HitData에서 Victim 반환
                else
                    return null;

            case BuffTargetType.Attacker:
                if (hit.Attacker != null)
                    return hit.Attacker; // HitData에서 Attacker 반환
                else
                    return null;

            case BuffTargetType.Hit:
                return hit;

            // 필요한 다른 BuffTargetType에 대한 로직 추가
            default:
                return null;
        }
    }

    // HitData가 BuffData의 HitFilter 조건에 맞는지 확인하는 메서드
    private bool IsValidHit(HitData hitData)
    {
        // BuffData의 HitFilter를 참조하여 유효한지 확인
        var hitFilter = buffData.Units.FirstOrDefault()?.Trigger.HitFilter; // 첫 번째 BuffUnitData의 HitFilter 사용
        if (hitFilter != null)
        {
            // HitType을 비트 플래그로 변환
            HitTypeFilter hitTypeAsFilter = (HitTypeFilter)(1 << (int)hitData.HitType);

            // hitType이 hitFilter에 포함되는지 확인
            if ((hitFilter.HitType & hitTypeAsFilter) == 0)
            {
                return false; // hitType이 hitFilter에 포함되지 않으면 false 반환
            }
        }
        return true; // 조건에 맞는 Hit
    }


    // Buff 효과 발동
    public void ExecuteEffect(List<BuffEffectData> effects, object target)
    {
        CharacterBehaviour targetCharacter = target as CharacterBehaviour;
        BulletData targetBullet = target as BulletData;
        BuffInstance targetBuff = target as BuffInstance;
        HitData targetHit = target as HitData;
        ActionData targetAction = target as ActionData;
        
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
                    targetCharacter.Heal(effect.Value);
                    break;

                case BuffEffectType.Character_HealPercent:
                    targetCharacter.HealPercent(effect.Value);
                    break;

                case BuffEffectType.Character_AddBuff:
                    BuffData newBuffData = effect.BuffData;
                    BuffManager.Instance.AddBuff(newBuffData, targetCharacter);
                    break;

                case BuffEffectType.Character_SetHp:
                    targetCharacter.SetHp(effect.Value);
                    break;

                case BuffEffectType.Character_InstantDamage:
                    HitData hit = new();
                    hit.HitDamage = effect.Value;
                    hit.HitType = HitType.DamageOnly;
                    hit.Attacker = owner;
                    hit.Victim = targetCharacter;
                    hit.HitApplyOwnerResource = false;
                    //hit.HitApplyBuffs = new();
                    hit.Direction = Vector3.zero;
                    targetCharacter.TakeDamage(hit);
                    break;

                case BuffEffectType.Character_AddResource:
                    targetCharacter.resourceTable.AddResource(effect.CharacterResourceKey, (int)effect.Value);
                    break;
                case BuffEffectType.Character_SetResource:
                    targetCharacter.resourceTable.SetResource(effect.CharacterResourceKey, (int)effect.Value);
                    break;
                case BuffEffectType.Buff_AddStack:
                    targetBuff.AddStack((int)effect.Value);
                    break;

                case BuffEffectType.Hit_AttackPowerPercent:
                    targetHit.HitDamage *= ( 1 + (effect.Value) / (100 + Mathf.Abs(effect.Value)));
                    break;
                case BuffEffectType.Hit_DamageZero:
                    targetHit.HitDamage = 0f;
                    break;
                case BuffEffectType.Hit_KnockBackPower:
                    targetHit.KnockbackPower *= (1 + (effect.Value) / (100 + Mathf.Abs(effect.Value)));
                    break;
                case BuffEffectType.Hit_KnockBackIgnore:
                    targetHit.KnockbackPower = 0f;
                    break;
                
                case BuffEffectType.Action_AttackPowerPercent:
                    foreach (var actionHit in owner.currentActionData.HitIdList)
                    {
                        actionHit.HitDamage *= (1 + (effect.Value) / (100 + Mathf.Abs(effect.Value)));
                    }
                    break;
                case BuffEffectType.Action_KnockBackPowerPercent:
                    foreach (var actionHit in owner.currentActionData.HitIdList)
                    {
                        actionHit.KnockbackPower *= (1 + (effect.Value) / (100 + Mathf.Abs(effect.Value)));
                    }
                    break;
                case BuffEffectType.Action_MoveAmountPercent:
                    foreach (var actionHit in owner.currentActionData.MovementList)
                    {
                        actionHit.StartValue *= (1 + (effect.Value) / (100 + Mathf.Abs(effect.Value)));
                        actionHit.EndValue *= (1 + (effect.Value) / (100 + Mathf.Abs(effect.Value)));
                    }
                    break;
                case BuffEffectType.Bullet_AttackPowerPercent:
                    foreach (var bulletHit in targetBullet.HitIdList)
                    {
                        bulletHit.HitDamage *= (1 + (effect.Value) / (100 + Mathf.Abs(effect.Value)));
                    }
                    break;
                case BuffEffectType.Bullet_SpeedPercent:
                    targetBullet.Speed *= (1 + effect.Value / (100 + Mathf.Abs(effect.Value)));
                    break;
                case BuffEffectType.Bullet_KnockBackPowerPercent:
                    foreach (var bulletHit in targetBullet.HitIdList)
                    {
                        bulletHit.KnockbackPower *= (1 + (effect.Value) / (100 + Mathf.Abs(effect.Value)));
                    }
                    break;
                default:
                    Debug.LogWarning($"Unknown BuffEffectType: {effect.Type}");
                    break;
            }
        }
    }

}
