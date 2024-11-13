using Mirror;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public class BulletBehaviour : NetworkBehaviour
{
    public BulletData bulletData;

    private float spawnTime;
    private float currentFrame;
    private Vector3 direction;
    private CharacterBehaviour owner;
    private bool isDespawned;
    private bool isHitStopped;
    private float hitStopTimer;

    private Dictionary<CharacterBehaviour, List<int>> hitTargets = new Dictionary<CharacterBehaviour, List<int>>();

    public void Initialize(CharacterBehaviour owner, Vector3 direction)
    {
        this.owner = owner;
        this.direction = direction.normalized;
        TriggerBullet(BulletTrigger.Spawn); // 초기 스폰 트리거 발생
        spawnTime = Time.time;
        currentFrame = 0;
    }

private void Update()
    {
        if (isHitStopped)
        {
            hitStopTimer -= Time.deltaTime;
            if (hitStopTimer <= 0)
                ResumeAfterHitStop();
            return;
        }

        CheckCollisionWithMap();
        BulletMove();
        CheckLifetime();
        HitCast();

        currentFrame += Time.deltaTime * 60f;
    }

    private void BulletMove()
    {
        switch (bulletData.MoveType)
        {
            case BulletMoveType.ConstantSpeed:
                transform.position += transform.forward * bulletData.Speed * Time.deltaTime;
                break;

                // 추후 다른 움직임 타입 추가 가능
        }
    }

    private void CheckLifetime()
    {
        if (bulletData.LiftTime > 0 && Time.time - spawnTime >= bulletData.LiftTime)
        {
            TriggerBullet(BulletTrigger.LifeTime);
        }
    }

    private void TriggerBullet(BulletTrigger trigger)
    {
        // 트리거가 DespawnBy 플래그에 포함되어 있고, 아직 Despawn되지 않았다면 Despawn 처리
        if (bulletData.DespawnBy.HasFlag(trigger) && !isDespawned)
        {
            isDespawned = true;
            TriggerDespawn();
            
        }

        // Trigger가 SpawnTrigger에 포함될 때 SpawnBullet 실행
        foreach (var spawnData in bulletData.BulletSpawnBulletList)
        {
            if (spawnData.SpawnTrigger.HasFlag(trigger))
            {
                SpawnBullet(spawnData);
            }
        }

        // Trigger가 SpawnTrigger에 포함될 때 SpawnVfx 실행
        foreach (var vfxData in bulletData.BulletSpawnVfxList)
        {
            if (vfxData.SpawnTrigger.HasFlag(trigger))
            {
                SpawnVfx(vfxData);
            }
        }
    }


    private void TriggerDespawn()
    {
        // Despawn 트리거 발생
        TriggerBullet(BulletTrigger.Despawn);

        // 실제 GameObject 소멸
        Destroy(gameObject);
    }

    private void SpawnBullet(BulletSpawnBulletData spawnData)
    {
        Vector3 spawnPosition = transform.position + transform.forward * spawnData.Offset.y + transform.right * spawnData.Offset.x;
        Quaternion spawnRotation = Quaternion.Euler(0, spawnData.Angle, 0) * transform.rotation;

        BulletBehaviour bullet = Instantiate(spawnData.BulletPrefab, spawnPosition, spawnRotation);
        bullet.Initialize(owner, spawnRotation * Vector3.forward);
    }
    private void SpawnVfx(BulletSpawnVfxData vfxData)
    {
        Vector3 spawnPosition = transform.position + transform.forward * vfxData.Offset.y + transform.right * vfxData.Offset.x;
        Quaternion spawnRotation = Quaternion.Euler(0, vfxData.Angle, 0) * transform.rotation;

        VfxObject vfx = Instantiate(vfxData.VfxPrefab, spawnPosition, spawnRotation);
        vfx.SetTransform(null, spawnPosition, spawnRotation, Vector3.one);
    }
    private void HitCast()
    {
        foreach (var hitbox in bulletData.HitboxList)
        {
            if (currentFrame >= hitbox.StartFrame && currentFrame <= hitbox.EndFrame)
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
                        var currentHit = bulletData.HitIdList.Find(hit => hit.HitId == hitbox.HitId);
                        HandleHit(hitbox.HitId, target, currentHit.HitStopFrame, currentHit.HitStunFrame, currentHit.hitType, currentHit.KnockbackPower, currentHit.HitApplyOwnerResource, currentHit.ResourceKey, currentHit.Value);

                        if (!hitTargets.ContainsKey(target))
                            hitTargets[target] = new List<int>();

                        hitTargets[target].Add(hitbox.HitGroup);
                        TriggerBullet(BulletTrigger.Hit);
                        break;
                    }
                }
            }
        }

        
    }
    //HandleHit(hitId, target, hitDamage, hitStopFrames, hitStunFrame, hitType, knockbackPower);
    private void HandleHit(int hitId, CharacterBehaviour target, float hitStopFrame, float hitStunFrame, HitType hitType, float knockbackPower, bool hitApplyOwnerResource, CharacterResourceKey key, int value)
    {
        HitData hitData = bulletData.HitIdList.Find(hit => hit.HitId == hitId);
        if (hitData == null) return;
        if (target == null) return;
        Vector3 hitDirection = (target.transform.position + direction.normalized - transform.position).normalized;

        //if (isServer)
        {
            target.TakeDamage(hitData.HitDamage, hitDirection, hitType, knockbackPower, hitStunFrame, owner);
            target.RpcTakeDamage(hitData.HitDamage, hitDirection, hitType, knockbackPower, hitStunFrame, owner);

            target.ApplyHitStop(hitData.HitStopFrame);
            target.RpcApplyHitStop(hitData.HitStopFrame);

            OnHit(target, hitApplyOwnerResource, key, value);
            RpcOnHit(target, hitApplyOwnerResource, key, value);
            ApplyHitStop(hitData.HitStopFrame);
        }
    }

    private void ApplyHitStop(float hitStopDuration)
    {
        float durationInSeconds = hitStopDuration / 60f;
        isHitStopped = true;
        hitStopTimer = durationInSeconds;  // 프레임을 초로 변환
    }

    private void ResumeAfterHitStop()
    {
        isHitStopped = false;
    }

    [ClientRpc]
    private void RpcOnHit(CharacterBehaviour target, bool hitApplyOwnerResource, CharacterResourceKey key, int value)
    {
        if (!isServer)
        {
            OnHit(target, hitApplyOwnerResource, key, value);
        }
    }

    private void OnHit(CharacterBehaviour target, bool hitApplyOwnerResource, CharacterResourceKey key, int value)
    {
        if (hitApplyOwnerResource)
            owner.resourceTable.AddResource(key, value);
    }

    private void CheckCollisionWithMap()
    {
        RaycastHit hit;
        LayerMask wallLayer = LayerMask.GetMask("WallCollider");

        if (Physics.SphereCast(transform.position, bulletData.BulletRadius, direction, out hit, bulletData.Speed * Time.deltaTime, wallLayer))
        {
            TriggerBullet(BulletTrigger.CollideMap);
            if (bulletData.ReflectOnWall)
                HandleCollisionResponse(hit.normal);
        }
    }

    private void HandleCollisionResponse(Vector3 collisionNormal)
    {
        // 충돌 시 반사 동작
        transform.forward = Vector3.Reflect(direction, collisionNormal); // 충돌 방향에 따른 반사
        direction = transform.forward; // 새 방향으로 설정
    }
}
