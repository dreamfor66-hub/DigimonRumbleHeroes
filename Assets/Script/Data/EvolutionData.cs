using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "EvolutionData", menuName = "Data/EvolutionData", order = 1)]
public class EvolutionData : ScriptableObject
{
    [PropertyOrder(0)]
    public CharacterData PrevData;
    [PropertyOrder(1)]
    [TableList(AlwaysExpanded =true)]
    public List<EvolutionInfo> EvolutionTree = new List<EvolutionInfo>(); 

}

[Serializable]
public class EvolutionInfo
{
    public CharacterBehaviour NextCharacter;
    [TableList(AlwaysExpanded = true)]
    [HideLabel]
    public List<EvolutionConditionData> Condition = new List<EvolutionConditionData>();
}

[Serializable]
public class EvolutionConditionData
{
    [HideLabel]
    public EvolutionConditionType Type;
    [HideLabel]
    public float Value;
} 

[Serializable]
public enum EvolutionConditionType
{
    None,
}
