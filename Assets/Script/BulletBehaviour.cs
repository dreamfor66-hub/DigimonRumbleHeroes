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

    //Character, 또는 기타 객체에 의해 Spawn 함수를 받아 소환된다.
    void Spawn()
    {
        //이곳에 코드를 작성해주세요.
    }

    void Despawn()
    {
        //BulletTrigger를 통해 despawn되는 경우입니다
    }

    public void OnHit()
    {
        //Character의 Action->Hit와 같이 동작 중 Hit될 경우 OnHitTrigger가 발생합니다.
    }
}
