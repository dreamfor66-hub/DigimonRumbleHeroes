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

    // 복사한 데이터
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

        // SerializedBulletData에서 복사하여 사용
        this.lifeTime = data.lifeTime;
        this.speed = data.speed;
        this.hitboxList = new List<HitboxData>(data.hitboxList);
        this.hitIdList = new List<HitData>(data.hitIdList);

        TriggerBullet(BulletTrigger.Spawn); // 초기 스폰 트리거 발생
        spawnTime = Time.time;
        currentFrame = 0;
    }
    [Command]
    public void CmdInitialize(NetworkIdentity ownerIdentity, Vector3 dir, SerializedBulletData data)
    {
        // 서버에서 초기화 데이터 설정
        CharacterBehaviour owner = ownerIdentity.GetComponent<CharacterBehaviour>();
        Initialize(owner, dir, data);

        // 클라이언트 동기화
        RpcInitialize(ownerIdentity, dir, data);
    }

    [ClientRpc]
    public void RpcInitialize(NetworkIdentity ownerIdentity, Vector3 dir, SerializedBulletData data)
    {
        if (isServer) return; // 서버에서는 이미 초기화되었으므로 클라이언트만 실행

        CharacterBehaviour owner = ownerIdentity.GetComponent<CharacterBehaviour>();
        Initialize(owner, dir, data); // 클라이언트 초기화
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

                // 추후 다른 움직임 타입 추가 가능
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

        // Trigger가 SpawnTrigger에 포함될 때 SpawnVfx 실행
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
        // Despawn 트리거 발생
        TriggerBullet(BulletTrigger.Despawn);
        RemoveIndicators();
        // 실제 GameObject 소멸
        StartCoroutine(DespawnWaitForServer());
    }

    private IEnumerator DespawnWaitForServer()
    {
        yield return null; // 1 프레임 대기
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

        // 서버에서 Bullet 생성
        GameObject bulletObject = Instantiate(bulletPrefab, position, Quaternion.LookRotation(this.direction));
        //BuffManager.Instance.TriggerBuffEffect(BuffTriggerType.OwnerSpawnBullet, clonedData);

        if (isServer)
            NetworkServer.Spawn(bulletObject);
        BulletBehaviour bullet = bulletObject.GetComponent<BulletBehaviour>();
        // Bullet 초기화
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

    /// <summary>
    /// Bullet의 Indicator를 생성합니다.
    /// </summary>
    private void HandleBulletIndicators()
    {
        foreach (var indicator in bulletData.BulletIndicators)
        {
            if (currentFrame >= indicator.StartFrame && currentFrame <= indicator.EndFrame)
            {
                if (!activeIndicators.Contains(indicator))
                {
                    // StartFrame에 도달하면 Indicator 생성
                    GameObject createdIndicator = CreateIndicator(indicator);
                    if (createdIndicator != null)
                    {
                        activeIndicators.Add(indicator);
                        activeIndicatorObjects[indicator] = createdIndicator;
                    }
                }

                // Indicator 업데이트
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
                // EndFrame 초과 시 Indicator 제거

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

        // 인디케이터 생성
        GameObject indicatorObject = Instantiate(prefab);
        var indicatorComponent = indicatorObject.GetComponent<IndicatorComponent>();
        if (indicatorComponent == null)
        {
            Debug.LogError("Indicator prefab does not have an IndicatorComponent.");
            Destroy(indicatorObject);
            return null;
        }

        // FollowTransform 여부에 따라 부모 설정
        if (indicator.FollowTransform)
        {
            indicatorObject.transform.SetParent(transform, false); // 캐릭터의 자식으로 설정
        }

        // StartPos와 EndPos 계산
        Vector3 startPosition = transform.position + transform.forward * indicator.StartPos.y + transform.right * indicator.StartPos.x;
        Vector3 endPosition = transform.position + transform.forward * indicator.EndPos.y + transform.right * indicator.EndPos.x;

        // 방향과 길이 계산
        Vector3 lineDirection = (endPosition - startPosition).normalized;
        float totalLength = Vector3.Distance(startPosition, endPosition);

        switch (indicator.Type)
        {
            case IndicatorType.Line:
                // Base 설정
                indicatorComponent.BaseTransform.position = startPosition;
                indicatorComponent.BaseTransform.rotation = Quaternion.LookRotation(lineDirection);
                indicatorComponent.BaseTransform.localScale = new Vector3(indicator.Width * 2, 1f, totalLength);

                // Fill 초기화
                if (indicatorComponent.FillTransform != null)
                {
                    indicatorComponent.FillTransform.position = startPosition;
                    indicatorComponent.FillTransform.rotation = Quaternion.LookRotation(lineDirection);
                    indicatorComponent.FillTransform.localScale = new Vector3(indicator.Width * 2, 1f, 0f); // 초기 Fill 길이 0
                }
                break;

            case IndicatorType.Circle:
                // Base 설정
                indicatorComponent.BaseTransform.position = startPosition;
                indicatorComponent.BaseTransform.localScale = new Vector3(indicator.Radius * 2, 1f, indicator.Radius * 2);

                // Fill 초기화
                if (indicatorComponent.FillTransform != null)
                {
                    indicatorComponent.FillTransform.position = startPosition;
                    indicatorComponent.FillTransform.localScale = new Vector3(0f, 1f, 0f); // 초기 Fill 크기 0
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
