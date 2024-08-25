using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameVariables", menuName = "Settings/GameVariables", order = 1)]
public class GameVariables : ScriptableObject
{

    public float dragThreshold = 12f;
    public float tapThreshold = 0.2f;
    public float maxDistance = 200f;

    // ���� �� �ʿ� ����ġ ���差�� ������ �� �ִ� ����Ʈ
    public List<int> expStockRequiredPerLevel = new List<int>();
    public List<int> expRequiredPerRank = new List<int>(); // ������ �ʿ��� ����ġ ����Ʈ

    // �������� �ʿ��� ����ġ ���差�� �������� �޼���
    public int GetExpStockRequiredForLevel(int level)
    {
        if (level >= 0 && level < expStockRequiredPerLevel.Count)
        {
            return expStockRequiredPerLevel[level];
        }
        return -1; // ������ ����Ʈ ������ ��� ��� ���� ó��
    }
}
