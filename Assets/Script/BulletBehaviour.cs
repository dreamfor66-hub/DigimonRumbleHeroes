using Mirror;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public class BulletBehaviour : NetworkBehaviour
{
    [SerializeField]
    [InlineEditor]
    public BulletData bulletData;

    private float spawnTime;
    private Vector3 direction;
    private CharacterBehaviour owner;
    private bool isDespawned;

    private Dictionary<CharacterBehaviour, List<int>> hitTargets = new Dictionary<CharacterBehaviour, List<int>>();

    public void Initialize(CharacterBehaviour owner, Vector3 direction)
    {
        this.owner = owner;
        this.direction = direction.normalized;
        TriggerBulletDespawn(BulletTrigger.Spawn);
        spawnTime = Time.time;
    }

    private void Update()
    {
        BulletMove();
        CheckLifetime();
        PerformHitDetection();
    }

    private void BulletMove()
    {
        switch (bulletData.MoveType)
        {
            case BulletMoveType.ConstantSpeed:
                transform.position += direction * bulletData.Speed * Time.deltaTime;
                break;

                // 추후 다른 움직임 타입 추가 가능
        }
    }

    private void CheckLifetime()
    {
        if (bulletData.LiftTime > 0 && Time.time - spawnTime >= bulletData.LiftTime)
        {
            TriggerBulletDespawn(BulletTrigger.LifeTime);
        }
    }

    private void TriggerBulletDespawn(BulletTrigger trigger)
    {
        if (bulletData.DespawnBy.HasFlag(trigger) && !isDespawned)
        {
            isDespawned = true;
            TriggerDespawn();
        }
    }

    private void TriggerDespawn()
    {
        // Despawn 트리거 발생
        TriggerBulletDespawn(BulletTrigger.Despawn);

        // 실제 GameObject 소멸
        Destroy(gameObject);
    }

    private void PerformHitDetection()
    {
        foreach (var hitbox in bulletData.HitboxList)
        {
            Vector3 hitboxPosition = transform.position + transform.right * hitbox.Offset.x + transform.forward * hitbox.Offset.y;
            Collider[] hitColliders = Physics.OverlapSphere(hitboxPosition, hitbox.Radius);

            foreach (var hitCollider in hitColliders)
            {
                CharacterBehaviour target = hitCollider.GetComponentInParent<CharacterBehaviour>();

                if (target == null || target == owner || (hitTargets.ContainsKey(target) && hitTargets[target].Contains(hitbox.HitGroup)))
                    continue;

                bool isValidTarget = (owner is PlayerController && target is EnemyController) ||
                                     (owner is EnemyController && target is PlayerController);

                if (isValidTarget)
                {
                    HandleHit(hitbox.HitId, target);

                    if (!hitTargets.ContainsKey(target))
                        hitTargets[target] = new List<int>();

                    hitTargets[target].Add(hitbox.HitGroup);
                    TriggerBulletDespawn(BulletTrigger.Hit);
                    break;
                }
            }
        }
    }

    private void HandleHit(int hitId, CharacterBehaviour target)
    {
        HitData hitData = bulletData.HitIdList.Find(hit => hit.HitId == hitId);
        if (hitData == null) return;

        Vector3 hitDirection = (target.transform.position - transform.position).normalized;

        if (isServer)
        {
            target.TakeDamage(hitData.HitDamage, hitDirection, hitData, owner);
            target.RpcTakeDamage(hitData.HitDamage, hitDirection, hitData, owner);
            OnHit(target);
            RpcOnHit(target);
            ApplyHitStop(hitData.HitStopFrame, target);
        }
    }

    private void ApplyHitStop(float hitStopDuration, CharacterBehaviour target)
    {
        if (isServer)
        {
            owner.ApplyHitStop(hitStopDuration);
            target.ApplyHitStop(hitStopDuration);
            RpcApplyHitStop(hitStopDuration);
        }
    }

    [ClientRpc]
    private void RpcApplyHitStop(float hitStopDuration)
    {
        if (!isServer)
        {
            owner.ApplyHitStop(hitStopDuration);
        }
    }

    [ClientRpc]
    private void RpcOnHit(CharacterBehaviour target)
    {
        if (!isServer)
        {
            OnHit(target);
        }
    }

    private void OnHit(CharacterBehaviour target)
    {
        Debug.Log($"Bullet hit {target.name} for damage with HitStop");
    }
}
