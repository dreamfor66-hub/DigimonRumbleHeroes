using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.Animations;
using System;

[CreateAssetMenu(fileName = "Data_Action_New", menuName = "Data/Action Data", order = 1)]
public class ActionData : ScriptableObject
{
   [PropertyOrder(0)]
    public int ActionFrame;

    [PropertyOrder(0)]
    public string AnimationKey;

    [PropertyOrder(0)]
    public AnimationCurve AnimationCurve;

    [PropertyOrder(0)]
    [Button("Reset AnimationCurve")]
    private void ResetAnimationCurve()
    {
        // AnimationCurve 초기화 로직
        InitializeAnimationCurve();
    }

    [Title("Move")]
    [PropertyOrder(1)]
    [TableList(AlwaysExpanded = true)]
    public List<MovementData> MovementList = new List<MovementData>();
    
    [PropertyOrder(1)]
    [TableList(AlwaysExpanded = true)]
    public List<SpecialMovementData> SpecialMovementList = new List<SpecialMovementData>();
    
    [Title ("Transition")]
    [PropertyOrder(1)]
    [TableList(AlwaysExpanded = true)]
    public List<TransitionData> Transition = new List<TransitionData>();

    [Title("Hitboxes")]
    [PropertyOrder(1)]
    [TableList(AlwaysExpanded = true)]
    public List<HitboxData> HitboxList = new List<HitboxData>();

    [HideLabel]
    [PropertyOrder(1)]
    public List<HitData> HitIdList = new List<HitData>();

    [Title("SpawnBullet")]
    [PropertyOrder(2)]
    [TableList(AlwaysExpanded = true)]
    public List<ActionSpawnBulletData> ActionSpawnBulletList = new List<ActionSpawnBulletData>();
    
    [Title("SpawnVfx")]
    [PropertyOrder(2)]
    [TableList(AlwaysExpanded = true)]
    public List<ActionSpawnVfxData> ActionSpawnVfxList = new List<ActionSpawnVfxData>();
    
    [Title("Indicator")]
    [PropertyOrder(2)]
    public List<IndicatorData> ActionIndicators = new List<IndicatorData>();

    [Title("Condition")]
    [Tooltip("액션 진입 조건. And 연산")]
    [PropertyOrder(3)]
    [TableList] public List<ActionConditionData> Conditions = new();

    [Title("Resource")]
    [PropertyOrder(3)]
    [TableList] public List<ActionUseResourceData> Resources = new();

    [Title("Auto Correction")]
    [PropertyOrder(10)]
    public bool canLookInAction;
    [PropertyOrder(10)]
    [HideLabel]
    public AutoCorrectionData AutoCorrection;

    /// <summary>
    /// 커브 생성버튼
    /// </summary>
    private void InitializeAnimationCurve()
    {
        // 1. ActionFrame을 x축으로 설정
        int totalFrames = ActionFrame;

        // 2. ActionData 이름을 기반으로 애니메이션 컨트롤러 검색
        string[] pathParts = name.Split('_');
        if (pathParts.Length < 3)
        {
            Debug.LogError("ActionData 이름 형식이 올바르지 않습니다.");
            return;
        }

        string namePattern = pathParts[2];
        string[] guids = AssetDatabase.FindAssets($"t:AnimatorController {namePattern}");

        if (guids.Length == 0)
        {
            Debug.LogError($"'{namePattern}'을 포함하는 애니메이션 컨트롤러를 찾을 수 없습니다.");
            return;
        }

        // 첫 번째 검색 결과 사용 (여러 개 있을 경우 첫 번째)
        string controllerPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

        if (controller == null)
        {
            Debug.LogError($"애니메이션 컨트롤러를 로드할 수 없습니다: {controllerPath}");
            return;
        }

        // 3. AnimationKey와 일치하는 상태(State)에서 애니메이션 클립 찾기
        AnimationClip clip = null;
        foreach (var layer in controller.layers)
        {
            foreach (var state in layer.stateMachine.states)
            {
                if (state.state.name == AnimationKey)
                {
                    clip = state.state.motion as AnimationClip;
                    break;
                }
            }
            if (clip != null)
                break;
        }

        if (clip == null)
        {
            Debug.LogError($"애니메이션 클립을 찾을 수 없습니다: {AnimationKey}");
            return;
        }

        int animationClipFrames = Mathf.RoundToInt(clip.length * clip.frameRate);

        // 4. AnimationCurve 초기화
        AnimationCurve = new AnimationCurve();
        AnimationCurve.AddKey(0f, 0f);  // 첫 번째 키프레임 (0,0)
        AnimationCurve.AddKey(totalFrames, animationClipFrames);  // 마지막 키프레임 (ActionFrame, 애니메이션 프레임 길이)

        for (int i = 0; i < AnimationCurve.keys.Length; i++)
        {
            AnimationCurve.SmoothTangents(i, 0);  // 선형 곡선으로 설정
        }

        Debug.Log("AnimationCurve가 초기화되었습니다.");
    }

    //[HideInInspector]
    public ActionData Clone()
    {
        // 얕은 복사 수행
        ActionData clonedAction = (ActionData)MemberwiseClone();

        // 깊은 복사 수행: 각 리스트를 새로 생성하고 원소들을 개별 복사
        clonedAction.MovementList = new List<MovementData>();
        foreach (var move in MovementList)
        {
            clonedAction.MovementList.Add(move.Clone());
        }
        // HitIdList 복사
        clonedAction.HitIdList = new List<HitData>();
        foreach (var hit in HitIdList)
        {
            clonedAction.HitIdList.Add(hit.Clone()); // HitData에 Clone() 메서드가 있다고 가정
        }

        clonedAction.SpecialMovementList = new List<SpecialMovementData>(SpecialMovementList);
        clonedAction.HitboxList = new List<HitboxData>(HitboxList);
        clonedAction.ActionSpawnBulletList = new List<ActionSpawnBulletData>();
        foreach(var bullet in ActionSpawnBulletList)
        {
            clonedAction.ActionSpawnBulletList.Add(bullet.Clone());
        }
        clonedAction.ActionSpawnVfxList = new List<ActionSpawnVfxData>(ActionSpawnVfxList);
        clonedAction.Conditions = new List<ActionConditionData>(Conditions);
        clonedAction.Resources = new List<ActionUseResourceData>(Resources);

        return clonedAction;
    }
}

// Inner Classes
[System.Serializable]
public class MovementData
{
    public int StartFrame;
    public int EndFrame;
    public Vector2 StartValue;
    public Vector2 EndValue;

    public MovementData Clone()
    {
        return (MovementData)MemberwiseClone();
    }
}

[System.Serializable]
public class TransitionData
{
    public int StartFrame;
    public int EndFrame;
    public TransitionType Type;
    public InputMessage InputType = InputMessage.A;
    public ActionKey NextAction = ActionKey.Basic01;
}


[System.Serializable]
public class IndicatorData
{
    public int StartFrame;
    public int EndFrame;
    public IndicatorType Type;
    public bool ShowMaxOnly;
    public bool FollowTransform;
    public Vector2 StartPos;
    [ShowIf("@Type == IndicatorType.Line")]
    public Vector2 EndPos;
    [ShowIf("@Type == IndicatorType.Line")]
    public float Width;
    [ShowIf("@Type == IndicatorType.Circle")]
    public float Radius;
}

public enum IndicatorType
{
    Line,
    Circle,
}


[System.Serializable]
public class SpecialMovementData
{
    public int StartFrame;
    public int EndFrame;
    public SpecialMovementType MoveType;
    [ShowIf("MoveType", SpecialMovementType.AddInput)]
    public bool CanRotate;
    public float Value;
}

public enum SpecialMovementType
{
    AddInput = 0,
    LookRotateTarget = 1,
}


[System.Serializable]
public class HitboxData
{
    public int StartFrame;
    public int EndFrame;
    public Vector2 Offset;
    public float Radius;
    public int HitId;
    public int HitGroup;
}

[System.Serializable]
public class HitData : CloneHelper<HitData>
{
    public int HitId;
    public float HitDamage;
    public float HitStopFrame;
    public float HitStunFrame;
    public float KnockbackPower;
    public HitType HitType;
    public bool HitApplyOwnerResource;
    [ShowIf(nameof(HitApplyOwnerResource), true)]
    public CharacterResourceKey ResourceKey;
    [ShowIf(nameof(HitApplyOwnerResource), true)]
    public int Value;
    //public List<BuffData> HitApplyBuffs = new();

    [HideInInspector] public CharacterBehaviour Attacker; // 공격자를 추적하기 위한 필드 추가
    [HideInInspector] public CharacterBehaviour Victim;   // 피격자를 추적하기 위한 필드 추가
    [HideInInspector] public Vector3 Direction;   // 피격자를 추적하기 위한 필드 추가
}


[System.Serializable]
public class ActionSpawnBulletData : CloneHelper<ActionSpawnBulletData>
{
    public int SpawnFrame;
    public BulletBehaviour BulletPrefab;
    public Vector2 Offset;
    public SpawnAnchor Anchor;
    public ActionSpawnBulletAnglePivot Pivot;
    public float Angle;
}

[System.Serializable]
public class ActionSpawnVfxData
{
    public int SpawnFrame;
    public VfxObject VfxPrefab;
    public Vector2 Offset;
    public float Angle;
    [HideInInspector]
    public bool HasSpawned = false; // 발사 여부를 추적하는 변수
}

public enum SpawnAnchor
{
    ThisCharacter = 0,
    Target = 1,
}
public enum ActionSpawnBulletAnglePivot
{
    Forward = 0,
    ToTarget = 1,
}
public enum ActionConditionType
{
    HasResource,
    //BulletExist,
    //BulletNotExist,
    //PropLessThan,
}

public enum TransitionType
{
    Always,
    Input,
}

[System.Serializable]
public class ActionConditionData
{
    public ActionConditionType Type;

    [ShowIf(nameof(Type), ActionConditionType.HasResource)]
    public CharacterResourceKey ResourceKey;

    [ShowIf(nameof(Type), ActionConditionType.HasResource)]
    public int Count;

    //[ShowIf("@Type == ActionConditionType.BulletExist || Type == ActionConditionType.BulletNotExist || Type == ActionConditionType.PropLessThan ")]
    //[FormerlySerializedAs("BulletKey")]
    //public string EntityKey;
}

[System.Serializable]
public class ActionUseResourceData
{
    public int Frame;
    public CharacterResourceKey ResourceKey;
    public int Count;
}

[System.Serializable]
public class AutoCorrectionData
{
    [HorizontalGroup("AutoCorrection")]
    [HideLabel]
    public AutoCorrectionType correctionType;

    [HorizontalGroup("AutoCorrection")]
    [ShowIf("correctionType", AutoCorrectionType.Entity)]
    [LabelText("Entity Name")]
    public string entityName;

    [HorizontalGroup("AutoCorrection")]
    [ShowIf("correctionType", AutoCorrectionType.Character)]
    [LabelText("Character Name")]
    public string characterName;
}

