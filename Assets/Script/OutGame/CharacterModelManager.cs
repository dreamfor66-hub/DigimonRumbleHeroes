using UnityEngine;

public class CharacterModelManager : MonoBehaviour
{
    private GameObject character3DModel;  // 인스턴스화된 캐릭터 모델

    // 캐릭터 3D 모델 초기화 메서드
    public void InitializeCharacter3DModel(CharacterItemInfo character)
    {
        //// 기존 캐릭터 3D 모델이 있다면 삭제
        //if (character3DModel != null)
        //{
        //    Destroy(character3DModel);
        //}

        //// ModelTable에서 해당 캐릭터 데이터에 매핑된 모델과 애니메이터를 가져옴
        //var modelMapping = ResourceHolder.Instance.characterVisualMap.GetModelMapping(character.characterData);
        //if (modelMapping == null)
        //{
        //    Debug.LogError($"ModelMapping not found for {character.characterData.name}");
        //    return;
        //}

        //// 캐릭터 모델 프리팹을 인스턴스화
        //character3DModel = Instantiate(modelMapping.characterPrefab);

        //// Animator 설정
        //Animator animator = character3DModel.GetComponent<Animator>();
        //if (animator == null)
        //{
        //    // Animator가 없으면 추가
        //    animator = character3DModel.AddComponent<Animator>();
        //}

        //// Animator에 RuntimeAnimatorController 설정
        //animator.runtimeAnimatorController = modelMapping.animator;

        //// 캐릭터 모델을 카메라의 자식으로 설정
        //character3DModel.transform.SetParent(this.transform);

        //// 모델 위치와 회전 초기화 (필요에 따라 조정)
        //character3DModel.transform.localPosition = Vector3.zero;
        //character3DModel.transform.localRotation = Quaternion.identity;
    }
}
