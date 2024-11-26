using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class BotAIState
{
    public CharacterState state;

    [MinMaxSlider(0, 20, showFields: true)]  // �Ÿ� ������ �����̴��� ǥ��
    public Vector2 distanceRange;

    public CharacterState nextState;

    // nextState�� Action�� �ƴ� ���� duration�� ǥ��
    [ShowIf("@nextState != CharacterState.Action")]
    public float duration;                // ������ ���� �ð�

    // nextState�� Action�� ���� actionKey�� ǥ��
    [ShowIf("@nextState == CharacterState.Action")]
    public ActionKey actionKey;

    [ShowIf("@nextState == CharacterState.Action")]
    public float cooldown;

    // ��ٿ� ���¸� �����ϱ� ���� ���� Ÿ�̸�
    [HideInInspector]
    public float cooldownTimer = 0f;

    // ��ٿ� ������ Ȯ���ϴ� �޼���
    public bool IsOnCooldown()
    {
        return cooldownTimer > 0f;
    }

    // ��ٿ� Ÿ�̸Ӹ� ������Ʈ�ϴ� �޼���
    public void UpdateCooldown(float deltaTime)
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= deltaTime;
        }
    }
}