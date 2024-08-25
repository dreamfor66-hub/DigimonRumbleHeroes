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
    //0은 스탯타입
    AttackType = 1,
    SpeedType = 2,
    PhysicalType = 3,
    HealType = 4,
    BalancedType = 5,

    //00은 데이터 / 백신 / 바이러스 / 프리 등
    Data = 10,
    Vaccine = 11,
    Virus = 12,
    Free = 13,

    //000은 종족타입. 이 종족타입은 대분류처럼만 사용(수기사형 등은 짐승형에 퉁치려함)
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
