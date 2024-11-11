using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.Animations;

[CreateAssetMenu(fileName = "Data_Buff_New", menuName = "Data/Buff Data", order = 1)]
public class BuffData : ScriptableObject
{
    [Title("Condition")]
    [PropertyOrder(0)]
    [TableList(AlwaysExpanded =true)]
    public List<BuffConditionData> BuffConditionData = new List<BuffConditionData>();
    
    [Title("Trigger")]
    [PropertyOrder(1)]
    public BuffTriggerType Trigger;
}

// Inner Classes
[System.Serializable]
public class BuffConditionData
{
    public BuffConditionType Type;
    [ShowIf(nameof(Type), BuffConditionType.HasResource)]
    public CharacterResourceKey ResourceKey;
    
    [ShowIf(nameof(Type), BuffConditionType.CurrentState)]
    public ActionKey ActionKey;
    
}

[System.Serializable]
public enum BuffConditionType
{
    Always = 0,
    HasResource = 1,
    CurrentState = 10,
}

//trigger
[System.Serializable]
public enum BuffTriggerType
{
    None,
    OwnerHit = 1,
    OwnerHurt = 10,
}


