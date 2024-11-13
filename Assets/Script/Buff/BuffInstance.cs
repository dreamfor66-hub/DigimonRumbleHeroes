using System.Collections.Generic;
using UnityEngine;

public class BuffInstance
{
    public int BuffId { get; private set; }
    public CharacterBehaviour owner { get; private set; }
    public BuffData buffData;
    private float duration;
    private int stackCount; // ���� ������ ���� ��
    private int maxStackCount; // �ִ� ���� ��

    private Dictionary<BuffTriggerType, int> triggerCounts = new Dictionary<BuffTriggerType, int>();
    private float fSecondTimer = 0f; // EveryFSecond�� Ÿ�̸�

    public bool IsExpired => duration <= 0;

    public BuffInstance(int id, BuffData data, CharacterBehaviour owner)
    {
        BuffId = id;
        buffData = data;
        this.owner = owner;
        duration = data.ReleaseData.Type == BuffReleaseType.Time ? data.ReleaseData.Time : float.MaxValue;
        
        stackCount = 1; // �⺻ ���� ���� 1
        maxStackCount = data.DuplicationData.MaxStackCount; // ������ �ִ� ���� ���� ������
    }

    public void Activate()
    {
        Debug.Log($"Buff {BuffId} activated on {owner.name}");
        TriggerEffect(BuffTriggerType.StartThisBuff);
    }

    // Buff ��Ȱ��ȭ
    public void Deactivate()
    {
        Debug.Log($"Buff {BuffId} deactivated on {owner.name}");
        TriggerEffect(BuffTriggerType.EndThisBuff); // ���� ���� �� Ʈ����
        // Buff ���� ȿ�� ����
    }

    /// ������ ���� ���� ���� �߰�
    public void AddStack(int count)
    {
        if (stackCount < maxStackCount)
        {
            stackCount += count;
            TriggerEffect(BuffTriggerType.StackThisBuff); // ���� ���� �� Ʈ����
            Debug.Log($"Buff {BuffId} stack increased: {stackCount}/{maxStackCount}");

            // ������ Ư�� ���� �����ϸ� Ʈ���� �ߵ�
            if (stackCount == buffData.DuplicationData.MaxStackCount)
            {
                TriggerEffect(BuffTriggerType.NStackofThisBuff); // Ư�� ���ÿ� ���� �� Ʈ����
            }
        }
    }

    // �ֱ������� ȣ��Ǵ� ������Ʈ �Լ�
    public void Update(float deltaTime)
    {
        duration -= deltaTime;
        // EveryFSecond Ʈ���� ó��
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

    // Ư�� Ʈ���� Ÿ���� Ȯ���ϴ� �޼��� �߰�
    public bool CheckTrigger(BuffTriggerType triggerType, HitType hitType = HitType.DamageOnly)
    {
        foreach (var unit in buffData.Units)
        {
            if (unit.Trigger.Type == triggerType && (unit.Trigger.HitFilter.HitType == hitType || hitType == HitType.All))
                return true;
        }
        return false;
    }

    // BuffUnit�� ���� üũ
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

    // ���� ���� �˻� �޼���
    private bool CheckCondition(BuffConditionData condition)
    {
        //��...�̷���
        if (condition.Type == BuffConditionType.OwnerHpPercent)
        {
            float hpPercent = 0; // ĳ������ ���� ü�� �ۼ�Ʈ ���
            return CompareValue(hpPercent, condition.Comparator, condition.ComparativeValueFixed);
        }
        return true;
    }

    // ���� ���ϴ� �޼���
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
        // Ʈ���� ȣ�� Ƚ�� ����
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
                    // BuffUnit�� ���ǵ� target�� �������� ȿ���� ����
                    triggerCounts[triggerType] = 0; // Ʈ���� Ƚ�� �ʱ�ȭ
                    var target = DetermineTarget(unit.Target);
                    ExecuteEffect(unit.Effects, target);
                }
            }
        }
    }

    private object DetermineTarget(BuffTargetData targetData)
    {
        // BuffTargetType�� ���� ������ ������Ʈ�� ã�� ������ �߰�
        switch (targetData.Type)
        {
            case BuffTargetType.Owner:
                return owner; // CharacterBehaviour Ÿ���� owner ��ȯ

            case BuffTargetType.ThisBuff:
                return this; // ���� BuffInstance �ڽ��� ��ȯ

            case BuffTargetType.Bullet:
                return GetRecentBullet(); // ���� �ֱٿ� �߻��� BulletBehaviour ��ü ��ȯ

            case BuffTargetType.Victim:
                return GetRecentVictim(); // ���� �ֱٿ� ���ظ� ���� ĳ���� ��ȯ...�̵��������ϴ°� �³�?

            case BuffTargetType.Attacker:
                return GetRecentAttacker(); // ���� �ֱ� ������ ĳ���� ��ȯ

            // �ʿ��� �ٸ� BuffTargetType�� ���� ���� �߰�
            default:
                return null;
        }
    }

    // Bullet�� Victim �� �ֱ� �߻��� ������Ʈ�� �������� ���� �޼���
    private BulletBehaviour GetRecentBullet()
    {
        // �ֱٿ� ������ Bullet�� �����ϴ� ������ BuffManager �Ǵ� BuffInstance�� ����
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

    // Buff ȿ�� �ߵ�
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
                    // Heal �޼��带 ���� ����� HP ȸ��
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
