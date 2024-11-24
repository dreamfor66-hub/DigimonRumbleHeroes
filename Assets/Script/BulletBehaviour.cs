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

    // ������ ������
    private float lifeTime;
    private float speed;
    private List<HitboxData> hitboxList = new List<HitboxData>();
    private List<HitData> hitIdList = new List<HitData>();
    private SerializedBulletData serializedBulletData;

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

    public void Initialize(CharacterBehaviour owner, Vector3 direction, SerializedBulletData data)
    {
        if (data == null)
            return;
        this.owner = owner;
        this.direction = direction.normalized;
        this.serializedBulletData = data;

        // SerializedBulletData���� �����Ͽ� ���
        this.lifeTime = data.lifeTime;
        this.speed = data.speed;
        this.hitboxList = new List<HitboxData>(data.hitboxList);
        this.hitIdList = new List<HitData>(data.hitIdList);

        TriggerBullet(BulletTrigger.Spawn); // �ʱ� ���� Ʈ���� �߻�
        spawnTime = Time.time;
        currentFrame = 0;
    }
    [Command]
    public void CmdInitialize(NetworkIdentity ownerIdentity, Vector3 dir, SerializedBulletData data)
    {
        // �������� �ʱ�ȭ ������ ����
        CharacterBehaviour owner = ownerIdentity.GetComponent<CharacterBehaviour>();
        Initialize(owner, dir, data);

        // Ŭ���̾�Ʈ ����ȭ
        RpcInitialize(ownerIdentity, dir, data);
    }

    [ClientRpc]
    public void RpcInitialize(NetworkIdentity ownerIdentity, Vector3 dir, SerializedBulletData data)
    {
        if (isServer) return; // ���������� �̹� �ʱ�ȭ�Ǿ����Ƿ� Ŭ���̾�Ʈ�� ����

        CharacterBehaviour owner = ownerIdentity.GetComponent<CharacterBehaviour>();
        Initialize(owner, dir, data); // Ŭ���̾�Ʈ �ʱ�ȭ
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
                transform.position += transform.forward * speed * Time.deltaTime;
                break;

                // ���� �ٸ� ������ Ÿ�� �߰� ����
        }
    }

    private void CheckLifetime()
    {
        if (lifeTime > 0 && Time.time - spawnTime >= lifeTime)
        {
            TriggerBullet(BulletTrigger.LifeTime);
        }
    }

    private void TriggerBullet(BulletTrigger trigger)
    {
        // Ʈ���Ű� DespawnBy �÷��׿� ���ԵǾ� �ְ�, ���� Despawn���� �ʾҴٸ� Despawn ó��
        if (bulletData.DespawnBy.HasFlag(trigger) && !isDespawned)
        {
            isDespawned = true;
            TriggerDespawn();
            
        }

        // Trigger�� SpawnTrigger�� ���Ե� �� SpawnBullet ����
        foreach (var spawnData in bulletData.BulletSpawnBulletList)
        {
            if (spawnData.SpawnTrigger.HasFlag(trigger))
            {
                SpawnBullet(spawnData);
            }
        }

        // Trigger�� SpawnTrigger�� ���Ե� �� SpawnVfx ����
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
        // Despawn Ʈ���� �߻�
        TriggerBullet(BulletTrigger.Despawn);

        // ���� GameObject �Ҹ�
        Destroy(gameObject);
    }

    private void SpawnBullet(BulletSpawnBulletData spawnData)
    {
        Vector3 spawnPosition = transform.position + transform.forward * spawnData.Offset.y + transform.right * spawnData.Offset.x;
        Quaternion spawnRotation = Quaternion.Euler(0, spawnData.Angle, 0) * transform.rotation;

        // spawnData���� ���� ������ Bullet�� ������ ��������
        BulletData newBulletData = spawnData.BulletPrefab.bulletData;

        SerializedBulletData serializedData = new SerializedBulletData(
            newBulletData.LiftTime,
            newBulletData.Speed,
            newBulletData.HitboxList,
            newBulletData.HitIdList
        );

        BulletBehaviour bullet = Instantiate(spawnData.BulletPrefab, spawnPosition, spawnRotation);
        //BuffManager.Instance.TriggerBuffEffect(BuffTriggerType.OwnerSpawnBullet, clonedData);
        bullet.Initialize(owner, spawnRotation * Vector3.forward, serializedData);
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
        foreach (var hitbox in hitboxList)
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
                        var hit = hitIdList.Find(x => x.HitId == hitbox.HitId).Clone();

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
        hitStopTimer = durationInSeconds;  // �������� �ʷ� ��ȯ
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
        // �浹 �� �ݻ� ����
        transform.forward = Vector3.Reflect(direction, collisionNormal); // �浹 ���⿡ ���� �ݻ�
        direction = transform.forward; // �� �������� ����
    }
}
