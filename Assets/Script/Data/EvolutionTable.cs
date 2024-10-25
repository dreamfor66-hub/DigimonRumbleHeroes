using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EvolutionTable", menuName = "Data/EvolutionTable", order = 1)]
public class EvolutionTable : ScriptableObject
{
    public List<EvolutionData> datas = new List<EvolutionData>();

    // 선택된 캐릭터의 가능한 진화 경로를 반환
    public List<EvolutionData> GetPossibleEvolutions(CharacterData characterData)
    {
        return datas.FindAll(e => e.prevCharacterData == characterData);
    }
}