using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.Animations;

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
        // AnimationCurve �ʱ�ȭ ����
        InitializeAnimationCurve();
    }

    [Title("Move")]
    [PropertyOrder(1)]
    [TableList(AlwaysExpanded = true)]
    public List<MovementData> MovementList = new List<MovementData>();
    
    [PropertyOrder(1)]
    [TableList(AlwaysExpanded = true)]
    public List<SpecialMovementData> SpecialMovementList = new List<SpecialMovementData>();

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
    public List<BulletSpawnData> ActionSpawnBulletList = new List<BulletSpawnData>();

    [Title("Auto Correction")]
    [PropertyOrder(10)]
    [HideLabel]
    public AutoCorrectionData AutoCorrection;

    /// <summary>
    /// Ŀ�� ������ư
    /// </summary>
    private void InitializeAnimationCurve()
    {
        // 1. ActionFrame�� x������ ����
        int totalFrames = ActionFrame;

        // 2. ActionData �̸��� ������� �ִϸ��̼� ��Ʈ�ѷ� �˻�
        string[] pathParts = name.Split('_');
        if (pathParts.Length < 3)
        {
            Debug.LogError("ActionData �̸� ������ �ùٸ��� �ʽ��ϴ�.");
            return;
        }

        string namePattern = pathParts[2];
        string[] guids = AssetDatabase.FindAssets($"t:AnimatorController {namePattern}");

        if (guids.Length == 0)
        {
            Debug.LogError($"'{namePattern}'�� �����ϴ� �ִϸ��̼� ��Ʈ�ѷ��� ã�� �� �����ϴ�.");
            return;
        }

        // ù ��° �˻� ��� ��� (���� �� ���� ��� ù ��°)
        string controllerPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

        if (controller == null)
        {
            Debug.LogError($"�ִϸ��̼� ��Ʈ�ѷ��� �ε��� �� �����ϴ�: {controllerPath}");
            return;
        }

        // 3. AnimationKey�� ��ġ�ϴ� ����(State)���� �ִϸ��̼� Ŭ�� ã��
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
            Debug.LogError($"�ִϸ��̼� Ŭ���� ã�� �� �����ϴ�: {AnimationKey}");
            return;
        }

        int animationClipFrames = Mathf.RoundToInt(clip.length * clip.frameRate);

        // 4. AnimationCurve �ʱ�ȭ
        AnimationCurve = new AnimationCurve();
        AnimationCurve.AddKey(0f, 0f);  // ù ��° Ű������ (0,0)
        AnimationCurve.AddKey(totalFrames, animationClipFrames);  // ������ Ű������ (ActionFrame, �ִϸ��̼� ������ ����)

        for (int i = 0; i < AnimationCurve.keys.Length; i++)
        {
            AnimationCurve.SmoothTangents(i, 0);  // ���� ����� ����
        }

        Debug.Log("AnimationCurve�� �ʱ�ȭ�Ǿ����ϴ�.");
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
}


[System.Serializable]
public class SpecialMovementData
{
    public int StartFrame;
    public int EndFrame;
    public SpecialMovementType MoveType;
}

public enum SpecialMovementType
{
    AddInput = 0,
    Rotate = 1,
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
public class HitData
{
    public int HitId;
    public float HitDamage;
    public float HitStopFrame;
    public float HitStunFrame;
    public float KnockbackPower;
    public HitType hitType;
}

[System.Serializable]
public class BulletSpawnData
{
    public int SpawnFrame;
    public BulletBehaviour BulletPrefab;
    public Vector2 Offset;
    public ActionSpawnBulletAnglePivot Pivot;
    public float Angle;
}

public enum ActionSpawnBulletAnglePivot
{
    Forward = 0,
    ToTarget = 1,
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

