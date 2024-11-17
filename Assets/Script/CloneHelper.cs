using System;

public abstract class CloneHelper<T> where T : CloneHelper<T>
{
    public T Clone()
    {
        // �� Ŭ���� ���ο����� MemberwiseClone()�� ȣ���� �� �ֵ��� ��ȣ�� ���� �޼��� ���
        return (T)this.MemberwiseClone();
    }
}