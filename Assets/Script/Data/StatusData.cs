using UnityEngine;

[System.Serializable]
public class CharacterStatusData
{
    public StatusType StatusType;  // ���� ����
    public float statusDuration;
    public float statusStartTime;      // ���� ���� �ð�
    public bool IsPermanent { get; private set; } // ���� ���� ���� Ȯ��
    public CharacterStatusData(StatusType statusType, float duration)
    {
        StatusType = statusType;

        if (duration < 0)
        {
            IsPermanent = true; // ���� ������ ������ ���¸� ����
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