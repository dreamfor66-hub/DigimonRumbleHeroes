using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "Data_Character_New", menuName = "Data/CharacterData", order = 1)]
public class CharacterData : ScriptableObject
{
    [Title("이름")] 
    [HideLabel]
    public string characterName;

    [Title("진화단계")]
    [HideLabel]
    [EnumToggleButtons]
    public CharacterItemForm characterItemForm;

    [Title("특성")]
    [HideLabel]
    public CharacterItemTrait characterItemTrait1;

    [HideLabel]
    public CharacterItemTrait characterItemTrait2;

    [HideLabel]
    public CharacterItemTrait characterItemTrait3;


    [Title("BaseStat")]
    public float baseHP = 10f;
    public float baseATK = 5;
    public float moveSpeed = 5;
    public float defaultBasicAttackCycle = 1.5f;

    [Title("Physics")]
    public float colliderRadius = 0.5f;
    public float mass = 10f;
    public float drag = 10f;
    public float attackRange = 1.5f;
    [TableList]
    public List<HurtBox> Hurtboxes;
    
    [Title("RigController")]
    public Vector3 rigOffset = new Vector3(0,0.5f,0);
    public float weightChangeSpeed = 1f;

    [Title("ActionTable")]
    [TableList(ShowIndexLabels = true, DrawScrollView = true)]
    public List<ActionTableEntry> ActionTable = new List<ActionTableEntry>();

    // 특정 ActionKey에 해당하는 ActionData 찾기
    public bool TryGetActionData(ActionKey actionKey, out ActionData actionData)
    {
        foreach (var entry in ActionTable)
        {
            if (entry.ActionKey == actionKey)
            {
                actionData = entry.ActionData;
                return true;
            }
        }
        actionData = null;
        return false;
    }

    [Title("Resource")]
    [TableList(AlwaysExpanded =true)]
    public List<CharacterResourceData> Resources = new();

    [Title("StartBuff")]
    public List<BuffData> Buffs = new();
    
    [Title("EvolutionInfo")]
    [TableList(AlwaysExpanded = true)]
    public List<EvolutionInfo> EvolutionInfos = new List<EvolutionInfo>();

    [Title("Targeting")]
    [Range(0f, 1f)]
    [LabelText("0은 거리 중심, 1은 각도 중심")]
    public float targetingWeightThreshold = 0.5f;

    [LabelText("넓고 짧은 부채꼴 : 거리")]
    public float shortDistance = 5f; // 짧은 거리
    [LabelText("넓고 짧은 부채꼴 : 각도")]
    public float wideAngle = 90f;    // 넓은 각도
    [LabelText("좁고 긴 부채꼴 : 각도")]
    public float narrowAngle = 30f;  // 좁은 각도
    [LabelText("좁고 긴 부채꼴 : 거리")]
    public float longDistance = 10f; // 긴 거리
}


[System.Serializable]
public class ActionTableEntry
{
    public ActionKey ActionKey;
    public ActionData ActionData;
}

[System.Serializable]
public struct HurtBox
{
    public Vector2 Offset; // HurtBox의 중심점 오프셋
    public float Radius;   // HurtBox의 반지름
}

[System.Serializable]
public class CharacterResourceData
{
    public CharacterResourceKey Key;
    public int Min;
    public int Max;
    public int Init;
    public CharacterResourceResetBy ResetBy;
}

public class CharacterResourceInfo
{
    public CharacterResourceData Data;  // Reference to resource data
    public int Count;  // Current count of the resource

    public CharacterResourceInfo(CharacterResourceData data)
    {
        Data = data;
        Count = data.Init;
    }
}

public class CharacterResourceTable
{
    private readonly List<CharacterResourceInfo> resources = new();

    public CharacterResourceTable(List<CharacterResourceData> data)
    {
        resources.AddRange(data.Select(CreateInfo));
    }
    private CharacterResourceInfo CreateInfo(CharacterResourceData data)
    {
        return new CharacterResourceInfo(data)
        {
            Data = data,
            Count = data.Init // 초기 Count를 Init 값으로 설정
        };
    }
    public bool HasResource(CharacterResourceKey key, int count)
    {
        var cur = resources.Find(x => x.Data.Key == key);
        return cur != null && cur.Count >= count;
    }

    public void AddResource(CharacterResourceKey key, int count)
    {
        var resource = resources.Find(x => x.Data.Key == key);
        if (resource != null)
            resource.Count = Mathf.Clamp(resource.Count + count, resource.Data.Min, resource.Data.Max);
    }
    
    public void SetResource(CharacterResourceKey key, int count)
    {
        var resource = resources.Find(x => x.Data.Key == key);
        if (resource != null)
            resource.Count = Mathf.Clamp(count, resource.Data.Min, resource.Data.Max);
    }
    public int GetResourceValue(CharacterResourceKey key)
    {
        var resource = resources.Find(x => x.Data.Key == key);
        return resource?.Count ?? 0; // 리소스를 찾지 못하면 기본값 0 반환
    }
    
    public int GetResourceMaxValue(CharacterResourceKey key)
    {
        var resource = resources.Find(x => x.Data.Key == key);
        return resource?.Data.Max ?? 0; // 리소스를 찾지 못하면 기본값 0 반환
    }
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