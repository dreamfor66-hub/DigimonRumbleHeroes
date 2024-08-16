using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.Animations;

[CreateAssetMenu(fileName = "Data_Action_New", menuName = "Data/Action Data", order = 1)]
public class ActionData : ScriptableObject
{
    [PropertyOrder(0)]
    public int ActionFrame;  // �׼��� ��ü ���� (������ ����)

    [PropertyOrder(0)]
    public string AnimationKey;  // �ִϸ��̼� ������Ʈ�� ��Ī

    [PropertyOrder(0)]
    public AnimationCurve AnimationCurve;  // �ִϸ��̼� Ŀ��

    [PropertyOrder(0)]
    [Button("Reset AnimationCurve")]
    private void ResetAnimationCurve()
    {
        // AnimationCurve �ʱ�ȭ ����
        InitializeAnimationCurve();
    }

    [System.Serializable]
    public class MovementData
    {
        public int StartFrame;  // ������ ���� ������

        public int EndFrame;  // ������ ���� ������

        public Vector2 StartValue;  // x, z�� ���� ��ġ/��

        public Vector2 EndValue;  // x, z�� ���� ��ġ/��
    }

    [Title("Move")]
    [PropertyOrder(1)]
    [TableList(AlwaysExpanded = true)]
    public List<MovementData> MovementList = new List<MovementData>();

    [System.Serializable]
    public class HitboxData
    {

        public int StartFrame;  // ��Ʈ�ڽ� ���� ������
        public int EndFrame;    // ��Ʈ�ڽ� ���� ������
        public Vector2 Offset;  // ��Ʈ�ڽ� ��ġ ������
        public float Radius;    // ��Ʈ�ڽ� �ݰ�
        public int HitId;       // ��Ʈ�ڽ� ID
        public int HitGroup;    // ��Ʈ �׷� ID �߰�
    }

    [Title("Hitboxes")]
    [PropertyOrder(1)]
    [TableList(AlwaysExpanded = true)]
    public List<HitboxData> HitboxList = new List<HitboxData>();

    [System.Serializable]
    public class HitData
    {
        public int HitId;  // ��Ʈ ID
        public float HitDamage;  // ��Ʈ ������
        public float HitstopTime;  // ��Ʈ ��ž �ð�
    }

    [HideLabel]
    [PropertyOrder(1)]
    public List<HitData> HitIdList = new List<HitData>();

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

