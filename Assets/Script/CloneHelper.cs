using System;

public abstract class CloneHelper<T> where T : CloneHelper<T>
{
    public T Clone()
    {
        // 이 클래스 내부에서만 MemberwiseClone()을 호출할 수 있도록 보호된 복사 메서드 사용
        return (T)this.MemberwiseClone();
    }
}