using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "Data_Character_New", menuName = "Data/CharacterData", order = 1)]
public class CharacterData : ScriptableObject
{
    [Title("Appearance")]
    [HorizontalGroup("Character", Width = 0.4f)] // A ����
    [PreviewField(220, ObjectFieldAlignment.Center)]
    [HideLabel]
    public Sprite characterSprite;

    [Title("")]
    [Title("�̸�")] 
    [HideLabel]
    [VerticalGroup("Character/RightSide")] // B ����
    public string characterName;

    [Title("�÷�")]
    [HideLabel]
    [EnumToggleButtons]
    [VerticalGroup("Character/RightSide")]
    public CharacterItemColor characterItemColor;

    [Title("��ȭ�ܰ�")]
    [HideLabel]
    [EnumToggleButtons]
    [VerticalGroup("Character/RightSide")]
    public CharacterItemForm characterItemForm;

    [Title("Ư��")]
    [HideLabel]
    [VerticalGroup("Character/RightSide")]
    [HorizontalGroup("Character/RightSide/Traits")]
    public CharacterItemTrait characterItemTrait1;

    [Title("")]
    [HideLabel]
    [VerticalGroup("Character/RightSide")]
    [HorizontalGroup("Character/RightSide/Traits")]
    public CharacterItemTrait characterItemTrait2;

    [Title("")]
    [HideLabel]
    [VerticalGroup("Character/RightSide")]
    [HorizontalGroup("Character/RightSide/Traits")]
    public CharacterItemTrait characterItemTrait3;


    [Title("BaseStat")]
    public float baseHP = 10f;
    public float baseATK = 5;
    public float moveSpeed = 5;
    public float defaultBasicAttackCycle = 1.5f;

    // ������ �� �����ϴ� HP�� ATK
    public float hpPerLevel = 1;
    public float atkPerLevel = 1;

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

    // Ư�� ActionKey�� �ش��ϴ� ActionData ã��
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

    [Title("Targeting")]
    [Range(0f, 1f)]
    [LabelText("0�� �Ÿ� �߽�, 1�� ���� �߽�")]
    public float targetingWeightThreshold = 0.5f;

    [LabelText("�а� ª�� ��ä�� : �Ÿ�")]
    public float shortDistance = 5f; // ª�� �Ÿ�
    [LabelText("�а� ª�� ��ä�� : ����")]
    public float wideAngle = 90f;    // ���� ����
    [LabelText("���� �� ��ä�� : ����")]
    public float narrowAngle = 30f;  // ���� ����
    [LabelText("���� �� ��ä�� : �Ÿ�")]
    public float longDistance = 10f; // �� �Ÿ�
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
    public Vector2 Offset; // HurtBox�� �߽��� ������
    public float Radius;   // HurtBox�� ������
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
            Count = data.Init // �ʱ� Count�� Init ������ ����
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
        return resource?.Count ?? 0; // ���ҽ��� ã�� ���ϸ� �⺻�� 0 ��ȯ
    }
    
    public int GetResourceMaxValue(CharacterResourceKey key)
    {
        var resource = resources.Find(x => x.Data.Key == key);
        return resource?.Data.Max ?? 0; // ���ҽ��� ã�� ���ϸ� �⺻�� 0 ��ȯ
    }
}