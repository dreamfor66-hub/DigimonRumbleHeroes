using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class BotAIState
{
    public CharacterState state;

    [MinMaxSlider(0, 20, showFields: true)]  // 거리 범위를 슬라이더로 표현
    public Vector2 distanceRange;

    public CharacterState nextState;

    // nextState가 Action이 아닐 때만 duration을 표시
    [ShowIf("@nextState != CharacterState.Action")]
    public float duration;                // 상태의 지속 시간

    // nextState가 Action일 때만 actionKey를 표시
    [ShowIf("@nextState == CharacterState.Action")]
    public ActionKey actionKey;
}