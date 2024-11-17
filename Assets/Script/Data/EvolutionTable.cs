using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EvolutionTable", menuName = "Data/EvolutionTable", order = 1)]
public class EvolutionTable : ScriptableObject
{
    public List<EvolutionData> datas = new List<EvolutionData>();
}