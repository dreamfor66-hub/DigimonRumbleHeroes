using UnityEngine;

public class CharacterModelManager : MonoBehaviour
{
    private GameObject character3DModel;  // �ν��Ͻ�ȭ�� ĳ���� ��

    // ĳ���� 3D �� �ʱ�ȭ �޼���
    public void InitializeCharacter3DModel(CharacterItemInfo character)
    {
        //// ���� ĳ���� 3D ���� �ִٸ� ����
        //if (character3DModel != null)
        //{
        //    Destroy(character3DModel);
        //}

        //// ModelTable���� �ش� ĳ���� �����Ϳ� ���ε� �𵨰� �ִϸ����͸� ������
        //var modelMapping = ResourceHolder.Instance.characterVisualMap.GetModelMapping(character.characterData);
        //if (modelMapping == null)
        //{
        //    Debug.LogError($"ModelMapping not found for {character.characterData.name}");
        //    return;
        //}

        //// ĳ���� �� �������� �ν��Ͻ�ȭ
        //character3DModel = Instantiate(modelMapping.characterPrefab);

        //// Animator ����
        //Animator animator = character3DModel.GetComponent<Animator>();
        //if (animator == null)
        //{
        //    // Animator�� ������ �߰�
        //    animator = character3DModel.AddComponent<Animator>();
        //}

        //// Animator�� RuntimeAnimatorController ����
        //animator.runtimeAnimatorController = modelMapping.animator;

        //// ĳ���� ���� ī�޶��� �ڽ����� ����
        //character3DModel.transform.SetParent(this.transform);

        //// �� ��ġ�� ȸ�� �ʱ�ȭ (�ʿ信 ���� ����)
        //character3DModel.transform.localPosition = Vector3.zero;
        //character3DModel.transform.localRotation = Quaternion.identity;
    }
}
