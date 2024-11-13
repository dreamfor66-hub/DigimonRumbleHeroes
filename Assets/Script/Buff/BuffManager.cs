using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class BuffManager : SingletonBehaviour<BuffManager>
{
    // Buff 저장소: Buff ID를 키로 사용하여 BuffInstance를 저장
    public Dictionary<int, BuffInstance> activeBuffs = new Dictionary<int, BuffInstance>();

    public override void Init()
    {
        // 필요한 초기화 작업이 있으면 여기에 작성합니다.
        Debug.Log("BuffManager initialized.");
    }
    // Buff 생성: BuffData와 Owner를 받아 새로운 BuffInstance를 생성 및 관리
    public int AddBuff(BuffData buffData, CharacterBehaviour owner)
    {
        // 중복 버프 확인
        BuffInstance existingBuff = FindExistingBuff(buffData, owner);

        // 중복 버프가 있고, 중복 처리가 StackCount로 설정된 경우 스택 증가
        if (existingBuff != null && buffData.DuplicationData.Type == BuffDuplicationType.StackCount)
        {
            existingBuff.AddStack(1);
            return existingBuff.BuffId;
        }

        // 고유 Buff ID 생성
        int buffId = GenerateUniqueBuffId();
        var newBuff = new BuffInstance(buffId, buffData, owner);

        // Buff 추가 및 활성화
        activeBuffs[buffId] = newBuff;
        newBuff.Activate();

        return buffId;
    }

    // 기존 버프 찾기: 같은 BuffData와 Owner를 가진 버프가 있는지 확인
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

    // Buff 제거: Buff ID로 찾아서 해제 후 제거
    public void RemoveBuff(int buffId)
    {
        if (activeBuffs.TryGetValue(buffId, out BuffInstance buffInstance))
        {
            buffInstance.Deactivate();
            activeBuffs.Remove(buffId);
        }
    }

    // 특정 캐릭터의 Buff 모두 제거
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

    // Buff 트리거: 특정 이벤트가 발생했을 때 트리거 조건을 만족하는 Buff 효과 발동
    public void TriggerBuffEffect(BuffTriggerType triggerType, object parameter = null)
    {
        foreach (var buffInstance in activeBuffs.Values)
        {
            // triggerType에 맞는 매개변수를 사용하여 BuffInstance에서 처리하도록 위임
            buffInstance.TriggerEffect(triggerType, parameter);
        }
    }

    // BuffManager가 Tick 단위로 Buff 업데이트를 관리
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

    // 고유 Buff ID 생성기
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




    // Inspector에서 확인용 리스트
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

// BuffInstance 클래스 (내부에서 관리하는 형태로 작성)
