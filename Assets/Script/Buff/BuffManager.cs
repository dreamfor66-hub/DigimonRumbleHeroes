using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class BuffManager : SingletonBehaviour<BuffManager>
{
    // Buff �����: Buff ID�� Ű�� ����Ͽ� BuffInstance�� ����
    public Dictionary<int, BuffInstance> activeBuffs = new Dictionary<int, BuffInstance>();

    public override void Init()
    {
        // �ʿ��� �ʱ�ȭ �۾��� ������ ���⿡ �ۼ��մϴ�.
        Debug.Log("BuffManager initialized.");
    }
    // Buff ����: BuffData�� Owner�� �޾� ���ο� BuffInstance�� ���� �� ����
    public int AddBuff(BuffData buffData, CharacterBehaviour owner)
    {
        // �ߺ� ���� Ȯ��
        BuffInstance existingBuff = FindExistingBuff(buffData, owner);

        // �ߺ� ������ �ְ�, �ߺ� ó���� StackCount�� ������ ��� ���� ����
        if (existingBuff != null && buffData.DuplicationData.Type == BuffDuplicationType.StackCount)
        {
            existingBuff.AddStack(1);
            return existingBuff.BuffId;
        }

        // ���� Buff ID ����
        int buffId = GenerateUniqueBuffId();
        var newBuff = new BuffInstance(buffId, buffData, owner);

        // Buff �߰� �� Ȱ��ȭ
        activeBuffs[buffId] = newBuff;
        newBuff.Activate();

        return buffId;
    }

    // ���� ���� ã��: ���� BuffData�� Owner�� ���� ������ �ִ��� Ȯ��
    private BuffInstance FindExistingBuff(BuffData buffData, CharacterBehaviour owner)
    {
        foreach (var buff in activeBuffs.Values)
        {
            if (buff.buffData == buffData && buff.owner == owner)
            {
                return buff;
            }
        }
        return null;
    }

    // Buff ����: Buff ID�� ã�Ƽ� ���� �� ����
    public void RemoveBuff(int buffId)
    {
        if (activeBuffs.TryGetValue(buffId, out BuffInstance buffInstance))
        {
            buffInstance.Deactivate();
            activeBuffs.Remove(buffId);
        }
    }

    // Ư�� ĳ������ Buff ��� ����
    public void RemoveAllBuffs(CharacterBehaviour owner)
    {
        foreach (var buff in new List<BuffInstance>(activeBuffs.Values))
        {
            if (buff.owner == owner)
            {
                RemoveBuff(buff.BuffId);
            }
        }
    }

    // Buff Ʈ����: Ư�� �̺�Ʈ�� �߻����� �� Ʈ���� ������ �����ϴ� Buff ȿ�� �ߵ�
    public void TriggerBuffEffect(BuffTriggerType triggerType, object parameter = null)
    {
        foreach (var buffInstance in activeBuffs.Values)
        {
            // triggerType�� �´� �Ű������� ����Ͽ� BuffInstance���� ó���ϵ��� ����
            buffInstance.TriggerEffect(triggerType, parameter);
        }
    }

    // BuffManager�� Tick ������ Buff ������Ʈ�� ����
    private void Update()
    {
        foreach (var buff in new List<BuffInstance>(activeBuffs.Values))
        {
            buff.Update(Time.deltaTime);
            if (buff.IsExpired)
            {
                RemoveBuff(buff.BuffId);
            }
        }
    }

    // ���� Buff ID ������
    private int GenerateUniqueBuffId()
    {
        return System.Guid.NewGuid().GetHashCode();
    }

    public void SpawnBullet(BulletBehaviour bulletPrefab, Vector3 position, Vector3 direction, CharacterBehaviour owner)
    {
        BulletBehaviour bullet = Instantiate(bulletPrefab, position, Quaternion.LookRotation(direction));
        bullet.Initialize(owner, direction);
        NetworkServer.Spawn(bullet.gameObject);
    }




    // Inspector���� Ȯ�ο� ����Ʈ
    [SerializeField, ReadOnly]
    private List<BuffDisplay> activeBuffsDisplay = new List<BuffDisplay>();

    private void OnValidate()
    {
        UpdateBuffDisplay();
    }
    private void UpdateBuffDisplay()
    {
        activeBuffsDisplay.Clear();
        foreach (var kvp in activeBuffs)
        {
            activeBuffsDisplay.Add(new BuffDisplay
            {
                BuffId = kvp.Key,
                OwnerName = kvp.Value.owner.name,
                BuffName = kvp.Value.buffData.name
            });
        }
    }

    [System.Serializable]
    private class BuffDisplay
    {
        public int BuffId;
        public string OwnerName;
        public string BuffName;
    }
}

// BuffInstance Ŭ���� (���ο��� �����ϴ� ���·� �ۼ�)
