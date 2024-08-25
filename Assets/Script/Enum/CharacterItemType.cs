public enum CharacterItemColor
{
    Red = 0,
    Blue = 1,
    Green = 2,
    Yellow = 3,
    Purple = 4,
    Black = 5,
    White = 6,
}

public enum CharacterItemTrait
{
    None = 0,
    //0�� ����Ÿ��
    AttackType = 1,
    SpeedType = 2,
    PhysicalType = 3,
    HealType = 4,
    BalancedType = 5,

    //00�� ������ / ��� / ���̷��� / ���� ��
    Data = 10,
    Vaccine = 11,
    Virus = 12,
    Free = 13,

    //000�� ����Ÿ��. �� ����Ÿ���� ��з�ó���� ���(������� ���� �������� ��ġ����)
    Beast = 100,
    Bird = 101,
    Reptile = 102,
    Insectoid = 103,
}

public enum CharacterItemForm
{
    None = 0,
    InTraining = 1,
    Rookie = 2,
    Champion = 3,
    Ultimate = 4,
}
