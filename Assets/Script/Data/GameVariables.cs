using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameVariables", menuName = "Settings/GameVariables", order = 1)]
public class GameVariables : ScriptableObject
{

    public float dragThreshold = 12f;
    public float tapThreshold = 0.2f;
    public float maxDistance = 200f;

    // 레벨 당 필요 경험치 스톡량을 설정할 수 있는 리스트
    public List<int> expStockRequiredPerLevel = new List<int>();
    public List<int> expRequiredPerRank = new List<int>(); // 레벨당 필요한 경험치 리스트

    // 레벨업에 필요한 경험치 스톡량을 가져오는 메서드
    public int GetExpStockRequiredForLevel(int level)
    {
        if (level >= 0 && level < expStockRequiredPerLevel.Count)
        {
            return expStockRequiredPerLevel[level];
        }
        return -1; // 레벨이 리스트 범위를 벗어난 경우 에러 처리
    }
}
