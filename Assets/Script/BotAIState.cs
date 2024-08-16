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
}