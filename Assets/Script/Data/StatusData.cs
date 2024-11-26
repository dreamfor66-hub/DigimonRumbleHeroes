using UnityEngine;

[System.Serializable]
public class CharacterStatusData
{
    public StatusType StatusType;  // 상태 유형
    public float statusDuration;
    public float statusStartTime;      // 상태 시작 시간
    public bool IsPermanent { get; private set; } // 영구 상태 여부 확인
    public CharacterStatusData(StatusType statusType, float duration)
    {
        StatusType = statusType;

        if (duration < 0)
        {
            IsPermanent = true; // 음수 값으로 무제한 상태를 설정
        }
        else
        {
            IsPermanent = false;
            this.statusDuration = duration;
            statusStartTime = Time.time;
        }
    }

    public bool IsExpired => !IsPermanent && Time.time >= statusStartTime + statusDuration;

    public void ExtendDuration(float extraDuration)
    {
        if (!IsPermanent)
        {
            statusDuration += extraDuration;
        }
    }
}