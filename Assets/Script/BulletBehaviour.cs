using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Mirror;

public class BulletBehaviour : MonoBehaviour
{
    [SerializeField]
    [InlineEditor]
    public BulletData bulletData;

    //Character, �Ǵ� ��Ÿ ��ü�� ���� Spawn �Լ��� �޾� ��ȯ�ȴ�.
    void Spawn()
    {
        //�̰��� �ڵ带 �ۼ����ּ���.
    }

    void Despawn()
    {
        //BulletTrigger�� ���� despawn�Ǵ� ����Դϴ�
    }

    public void OnHit()
    {
        //Character�� Action->Hit�� ���� ���� �� Hit�� ��� OnHitTrigger�� �߻��մϴ�.
    }
}
