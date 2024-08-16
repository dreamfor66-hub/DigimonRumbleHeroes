using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.Animations;

[CreateAssetMenu(fileName = "Data_Action_New", menuName = "Data/Action Data", order = 1)]
public class ActionData : ScriptableObject
{
    [PropertyOrder(0)]
    public int ActionFrame;  // 액션의 전체 길이 (프레임 단위)

    [PropertyOrder(0)]
    public string AnimationKey;  // 애니메이션 스테이트의 명칭

    [PropertyOrder(0)]
    public AnimationCurve AnimationCurve;  // 애니메이션 커브

    [PropertyOrder(0)]
    [Button("Reset AnimationCurve")]
    private void ResetAnimationCurve()
    {
        // AnimationCurve 초기화 로직
        InitializeAnimationCurve();
    }

    [System.Serializable]
    public class MovementData
    {
        public int StartFrame;  // 움직임 시작 프레임

        public int EndFrame;  // 움직임 종료 프레임

        public Vector2 StartValue;  // x, z축 시작 위치/값

        public Vector2 EndValue;  // x, z축 종료 위치/값
    }

    [Title("Move")]
    [PropertyOrder(1)]
    [TableList(AlwaysExpanded = true)]
    public List<MovementData> MovementList = new List<MovementData>();

    [System.Serializable]
    public class HitboxData
    {

        public int StartFrame;  // 히트박스 시작 프레임
        public int EndFrame;    // 히트박스 종료 프레임
        public Vector2 Offset;  // 히트박스 위치 오프셋
        public float Radius;    // 히트박스 반경
        public int HitId;       // 히트박스 ID
        public int HitGroup;    // 히트 그룹 ID 추가
    }

    [Title("Hitboxes")]
    [PropertyOrder(1)]
    [TableList(AlwaysExpanded = true)]
    public List<HitboxData> HitboxList = new List<HitboxData>();

    [System.Serializable]
    public class HitData
    {
        public int HitId;  // 히트 ID
        public float HitDamage;  // 히트 데미지
        public float HitstopTime;  // 히트 스탑 시간
    }

    [HideLabel]
    [PropertyOrder(1)]
    public List<HitData> HitIdList = new List<HitData>();

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
}

