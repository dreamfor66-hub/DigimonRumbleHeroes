using Mirror;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    private readonly HashSet<IndicatorData> activeIndicators = new();
    private readonly Dictionary<IndicatorData, GameObject> activeIndicatorObjects = new();

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

        HandleBulletIndicators();
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
                var bulletData = spawnData.BulletPrefab.bulletData;
                SerializedBulletData serializedData = new SerializedBulletData(
                    bulletData.LifeTime,
                    bulletData.Speed,
                    bulletData.HitboxList,
                    bulletData.HitIdList
                );

                var spawnPosition = transform.position + transform.forward * spawnData.Offset.y + transform.right * spawnData.Offset.x;
                var spawnRotation = Quaternion.Euler(0, spawnData.Angle, 0) * transform.forward;

                if (isServer)
                {
                    SpawnBullet(spawnPosition, spawnRotation, serializedData, spawnData.BulletPrefab.name);
                    //RpcSpawnBullet(spawnPosition, spawnRotation, serializedData, spawnData.BulletPrefab.name);
                }
                else
                    CmdSpawnBullet(spawnPosition, spawnRotation, serializedData, spawnData.BulletPrefab.name);
            }
        }

        // Trigger�� SpawnTrigger�� ���Ե� �� SpawnVfx ����
        foreach (var vfxData in bulletData.BulletSpawnVfxList)
        {
            if (vfxData.SpawnTrigger.HasFlag(trigger))
            {
                var spawnPosition = transform.position + transform.forward * vfxData.Offset.y + transform.right * vfxData.Offset.x;
                var spawnRotation = Quaternion.Euler(0, vfxData.Angle, 0) * transform.forward;
                if (isServer)
                {
                    SpawnVfx(spawnPosition, spawnRotation, vfxData.VfxPrefab.name);
                    //RpcSpawnVfx(spawnPosition, spawnRotation, vfxData.VfxPrefab.name);
                }
                else
                    CmdSpawnVfx(spawnPosition, spawnRotation, vfxData.VfxPrefab.name);
            }
        }
    }


    private void TriggerDespawn()
    {
        // Despawn Ʈ���� �߻�
        TriggerBullet(BulletTrigger.Despawn);
        RemoveIndicators();
        // ���� GameObject �Ҹ�
        StartCoroutine(DespawnWaitForServer());
    }

    private IEnumerator DespawnWaitForServer()
    {
        yield return null; // 1 ������ ���
        Destroy(gameObject);
    }

    public void SpawnBullet(Vector3 position, Vector3 direction, SerializedBulletData serializedData, string prefabName)
    {
        GameObject bulletPrefab = NetworkManager.singleton.spawnPrefabs.Find(prefab => prefab.name == prefabName);
        if (bulletPrefab == null)
        {
            Debug.LogError($"Prefab '{prefabName}' not found in RegisteredSpawnablePrefabs.");
            return;
        }

        // �������� Bullet ����
        GameObject bulletObject = Instantiate(bulletPrefab, position, Quaternion.LookRotation(this.direction));
        //BuffManager.Instance.TriggerBuffEffect(BuffTriggerType.OwnerSpawnBullet, clonedData);

        if (isServer)
            NetworkServer.Spawn(bulletObject);
        BulletBehaviour bullet = bulletObject.GetComponent<BulletBehaviour>();
        // Bullet �ʱ�ȭ
        if (bullet != null)
        {
            if (isServer)
            {
                bullet.Initialize(owner, direction, serializedData);
                bullet.RpcInitialize(owner.GetComponent<NetworkIdentity>(), this.direction, serializedData);
            }
            else
                bullet.CmdInitialize(owner.GetComponent<NetworkIdentity>(), this.direction, serializedData);
        }
    }

    [Command]
    public void CmdSpawnBullet(Vector3 position, Vector3 direction, SerializedBulletData serializedData, string prefabName)
    {
        SpawnBullet(position, direction, serializedData, prefabName);
        RpcSpawnBullet(position, direction, serializedData, prefabName);
    }

    [ClientRpc]
    public void RpcSpawnBullet(Vector3 position, Vector3 direction, SerializedBulletData serializedData, string prefabName)
    {
        if (!isServer)
        SpawnBullet(position, direction, serializedData, prefabName);
    }


    public void SpawnVfx(Vector3 position, Vector3 direction, string prefabName)
    {
        GameObject vfxPrefab = NetworkManager.singleton.spawnPrefabs.Find(prefab => prefab.name == prefabName);
        if (vfxPrefab == null)
        {
            Debug.LogError($"Prefab '{prefabName}' not found in RegisteredSpawnablePrefabs.");
            return;
        }

        GameObject vfxObject = Instantiate(vfxPrefab, position, Quaternion.Euler(direction));
        if (isServer)
        {
            NetworkServer.Spawn(vfxObject);
        }
        VfxObject vfx = vfxObject.GetComponent<VfxObject>();
        vfx.SetTransform(transform, position, Quaternion.Euler(direction), Vector3.one);
    }

    [Command]
    public void CmdSpawnVfx(Vector3 position, Vector3 direction, string prefabName)
    {
        SpawnVfx(position, direction, prefabName);
        RpcSpawnVfx(position, direction, prefabName);
    }

    [ClientRpc]
    public void RpcSpawnVfx(Vector3 position, Vector3 direction, string prefabName)
    {
        if (!isServer)
            SpawnVfx(position, direction, prefabName);
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
                        hit.Direction.y = 0;
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

    [ClientRpc]
    private void RpcHandleHit(HitData hit)
    {
        if (!isServer && !isLocalPlayer && hit.Victim != null)
        {
            HandleHit(hit);
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

    /// <summary>
    /// Bullet�� Indicator�� �����մϴ�.
    /// </summary>
    private void HandleBulletIndicators()
    {
        foreach (var indicator in bulletData.BulletIndicators)
        {
            if (currentFrame >= indicator.StartFrame && currentFrame <= indicator.EndFrame)
            {
                if (!activeIndicators.Contains(indicator))
                {
                    // StartFrame�� �����ϸ� Indicator ����
                    GameObject createdIndicator = CreateIndicator(indicator);
                    if (createdIndicator != null)
                    {
                        activeIndicators.Add(indicator);
                        activeIndicatorObjects[indicator] = createdIndicator;
                    }
                }

                // Indicator ������Ʈ
                if (activeIndicatorObjects.TryGetValue(indicator, out var indicatorObject))
                {
                    float progress = Mathf.Clamp01((currentFrame - indicator.StartFrame) / (float)(indicator.EndFrame - indicator.StartFrame));
                    if (indicator.ShowMaxOnly)
                        progress = 1;
                    UpdateIndicatorScale(indicatorObject, indicator, progress);
                }
            }
            else if (currentFrame > indicator.EndFrame)
            {
                // EndFrame �ʰ� �� Indicator ����

                if (activeIndicatorObjects.TryGetValue(indicator, out var indicatorObject))
                {
                    Destroy(indicatorObject);
                    activeIndicatorObjects.Remove(indicator);
                }
                activeIndicators.Remove(indicator);
                
            }
        }
    }

    private GameObject CreateIndicator(IndicatorData indicator)
    {
        GameObject prefab = ResourceHolder.Instance.GetIndicatorPrefab(indicator.Type);
        if (prefab == null)
        {
            Debug.LogError($"Indicator prefab not found for type: {indicator.Type}");
            return null;
        }

        // �ε������� ����
        GameObject indicatorObject = Instantiate(prefab);
        var indicatorComponent = indicatorObject.GetComponent<IndicatorComponent>();
        if (indicatorComponent == null)
        {
            Debug.LogError("Indicator prefab does not have an IndicatorComponent.");
            Destroy(indicatorObject);
            return null;
        }

        // FollowTransform ���ο� ���� �θ� ����
        if (indicator.FollowTransform)
        {
            indicatorObject.transform.SetParent(transform, false); // ĳ������ �ڽ����� ����
        }

        // StartPos�� EndPos ���
        Vector3 startPosition = transform.position + transform.forward * indicator.StartPos.y + transform.right * indicator.StartPos.x;
        Vector3 endPosition = transform.position + transform.forward * indicator.EndPos.y + transform.right * indicator.EndPos.x;

        // ����� ���� ���
        Vector3 lineDirection = (endPosition - startPosition).normalized;
        float totalLength = Vector3.Distance(startPosition, endPosition);

        switch (indicator.Type)
        {
            case IndicatorType.Line:
                // Base ����
                indicatorComponent.BaseTransform.position = startPosition;
                indicatorComponent.BaseTransform.rotation = Quaternion.LookRotation(lineDirection);
                indicatorComponent.BaseTransform.localScale = new Vector3(indicator.Width * 2, 1f, totalLength);

                // Fill �ʱ�ȭ
                if (indicatorComponent.FillTransform != null)
                {
                    indicatorComponent.FillTransform.position = startPosition;
                    indicatorComponent.FillTransform.rotation = Quaternion.LookRotation(lineDirection);
                    indicatorComponent.FillTransform.localScale = new Vector3(indicator.Width * 2, 1f, 0f); // �ʱ� Fill ���� 0
                }
                break;

            case IndicatorType.Circle:
                // Base ����
                indicatorComponent.BaseTransform.position = startPosition;
                indicatorComponent.BaseTransform.localScale = new Vector3(indicator.Radius * 2, 1f, indicator.Radius * 2);

                // Fill �ʱ�ȭ
                if (indicatorComponent.FillTransform != null)
                {
                    indicatorComponent.FillTransform.position = startPosition;
                    indicatorComponent.FillTransform.localScale = new Vector3(0f, 1f, 0f); // �ʱ� Fill ũ�� 0
                }
                break;
        }

        return indicatorObject;
    }

    private void UpdateIndicatorScale(GameObject indicatorObject, IndicatorData indicator, float progress)
    {
        var indicatorComponent = indicatorObject.GetComponent<IndicatorComponent>();
        if (indicatorComponent == null)
        {
            Debug.LogError("IndicatorObject does not have an IndicatorComponent.");
            return;
        }

        if (indicatorComponent.FillTransform == null)
        {
            Debug.LogError("FillTransform is missing in the IndicatorComponent.");
            return;
        }

        Vector3 currentScale = indicatorComponent.FillTransform.localScale;
        switch (indicator.Type)
        {
            case IndicatorType.Line:
                indicatorComponent.FillTransform.localScale = new Vector3(currentScale.x, currentScale.y, indicatorComponent.BaseTransform.localScale.z * progress);
                break;

            case IndicatorType.Circle:
                indicatorComponent.FillTransform.localScale = new Vector3(
                    indicatorComponent.BaseTransform.localScale.x * progress,
                    currentScale.y,
                    indicatorComponent.BaseTransform.localScale.z * progress);
                break;
        }
    }

    private void RemoveIndicators()
    {
        foreach (var indicator in new List<IndicatorData>(activeIndicators))
        {
            if (activeIndicatorObjects.TryGetValue(indicator, out var indicatorObject))
            {
                Destroy(indicatorObject);
                activeIndicatorObjects.Remove(indicator);
            }
            activeIndicators.Remove(indicator);
        }
    }
}
