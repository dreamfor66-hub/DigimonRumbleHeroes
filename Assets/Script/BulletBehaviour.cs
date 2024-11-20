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

    public bool IsValidTarget(CharacterBehaviour target)
    {
        if (target == null || owner == null) return false;

        // Validate target based on TeamType conditions
        return (owner.teamType == TeamType.Player && target.teamType == TeamType.Enemy) ||
               (owner.teamType == TeamType.Enemy && target.teamType == TeamType.Player) ||
               (owner.teamType == TeamType.Player && target.teamType == TeamType.Neutral) ||
               (owner.teamType == TeamType.Enemy && target.teamType == TeamType.Neutral) ||
               (owner.teamType == TeamType.Neutral && (target.teamType == TeamType.Player || target.teamType == TeamType.Enemy));
    }

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

        var originBulletData = spawnData.BulletPrefab.bulletData;
        var clonedData = originBulletData.Clone();

        BuffManager.Instance.TriggerBuffEffect(BuffTriggerType.OwnerSpawnBullet, clonedData);

        BulletBehaviour bullet = Instantiate(spawnData.BulletPrefab, spawnPosition, spawnRotation);
        bullet.Initialize(owner, spawnRotation * Vector3.forward);
    }

    private void SpawnVfx(BulletSpawnVfxData vfxData)
    {
        Vector3 spawnPosition = transform.position + transform.forward * vfxData.Offset.y + transform.right * vfxData.Offset.x;
        Quaternion spawnRotation = Quaternion.Euler(0, vfxData.Angle, 0) * transform.rotation;

        VfxObject vfx = Instantiate(vfxData.VfxPrefab, spawnPosition, spawnRotation);
        vfx.SetTransform(transform, spawnPosition, spawnRotation, Vector3.one);
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


                    if (IsValidTarget(target))
                    {
                        var hit = bulletData.HitIdList.Find(x => x.HitId == hitbox.HitId).Clone();

                        hit.Victim = target;
                        hit.Attacker = owner;
                        hit.Direction = (target.transform.position + direction.normalized - transform.position).normalized;
                        hit.HitDamage *= owner.characterData.baseATK;
                        HandleHit(hit);

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
    private void HandleHit(HitData hit)
    {
        if (hit == null) return;
        if (hit.Victim == null) return;

        if (isServer)
        {
            owner.OnHit(hit);
            owner.RpcOnHit(hit);

            hit.Victim.TakeDamage(hit);
            hit.Victim.RpcTakeDamage(hit);

            hit.Victim.ApplyHitStop(hit.HitStopFrame);
            hit.Victim.RpcApplyHitStop(hit.HitStopFrame);

            ApplyHitStop(hit.HitStopFrame);
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
