using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data_Character_New", menuName = "Data/CharacterData", order = 1)]
public class CharacterData : ScriptableObject
{
    [Title("Stat")]
    public float maxHealth = 10f;
    public float moveSpeed = 5f;
    public float attackPower = 5f;

    [Title("Physics")]
    public float colliderRadius = 0.5f;
    public float mass = 10f;
    public float drag = 10f;
    public float attackRange = 1.5f;
    [TableList]
    public List<HurtBox> Hurtboxes;

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