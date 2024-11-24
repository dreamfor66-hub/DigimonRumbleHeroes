using Mirror;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public abstract class CharacterBehaviour : NetworkBehaviour
{
    public CharacterData characterData;
    private Collider[] hurtboxColliders;

    protected Animator animator;
    protected Vector3 direction;
    [SerializeField]
    [SyncVar]
    protected float currentSpeed;
    protected float stopTime = 0.05f;
    protected float stopTimer;

    [SyncVar]
    public TeamType teamType;
    public bool IsValidTarget(CharacterBehaviour target)
    {
        if (target == null) return false;

        // Validate target based on TeamType conditions
        return (this.teamType == TeamType.Player && target.teamType == TeamType.Enemy) ||
               (this.teamType == TeamType.Enemy && target.teamType == TeamType.Player) ||
               (this.teamType == TeamType.Player && target.teamType == TeamType.Neutral) ||
               (this.teamType == TeamType.Enemy && target.teamType == TeamType.Neutral) ||
               (this.teamType == TeamType.Neutral && (target.teamType == TeamType.Player || target.teamType == TeamType.Enemy));
    }

    [SyncVar]
    public CharacterState currentState = CharacterState.Idle; // SyncVar로 변경하여 동기화
    [SyncVar]
    protected ActionKey currentActionKey;
    public ActionData currentActionData;

    [SyncVar]
    protected float currentFrame;
    [SyncVar]
    public bool isDie;

    [SerializeField,Sirenix.OdinInspector.ReadOnly, SyncVar]
    public float currentHealth;
    [SerializeField,Sirenix.OdinInspector.ReadOnly, SyncVar]
    public float maxHealth;

    protected Dictionary<CharacterBehaviour, List<int>> hitTargets;

    public SphereCollider collisionCollider;

    // Knockback 관련 변수들
    [SyncVar]
    private Vector3 knockBackDirection;
    [SyncVar] [SerializeField]
    private float initialKnockBackSpeed; //시작 속도
    [SyncVar] [SerializeField]
    private float currentknockBackSpeed; //현재 속도
    [SyncVar]
    private float knockBackDuration;
    [SyncVar]
    private float knockBackTimer;

    // HitStop 관련 변수들
    [SyncVar]
    private float hitStopTimer; // HitStop 시간을 관리하는 타이머
    [SyncVar]
    private bool isHitStopped; // HitStop 상태를 나타내는 변수

    // target 관련 변수
    [SyncVar]
    public CharacterBehaviour target;
    private GameObject targetIndicator;
    private RigController rigController;

    // ActionMove에 관한, player에서 주로 사용할 변수목록
    public Vector3 moveVector;
    public Quaternion targetRotation;
    public Vector3 initialTouchPosition;
    public Vector3 touchDelta;
    public float touchDeltaDistance;
    public float touchElapsedTime;
    public Vector3 inputDirection;
    public float speedMultiplier;
    public float touchStartTime;


    //vfx 관련 변수
    // Manual Vfx 관리용 리스트
    private List<VfxObject> activeManualVfxList = new List<VfxObject>();
    private HashSet<ActionSpawnVfxData> spawnedVfxData = new HashSet<ActionSpawnVfxData>();
    private HashSet<ActionSpawnBulletData> spawnedBulletData = new HashSet<ActionSpawnBulletData>();
    private HashSet<int> addedResourceFrames = new HashSet<int>();


    // hpBar UI
    private GameObject hpStaminaBarInstance; // 생성된 HP Bar 인스턴스
    protected HpStaminaBarController hpStaminaBarController;

    //resource 관련 변수
    public CharacterResourceTable resourceTable;

    protected virtual void Start()
    {
        animator = GetComponentInChildren<Animator>();
        currentSpeed = characterData.moveSpeed;
        hitTargets = new Dictionary<CharacterBehaviour, List<int>>();
        InitializeHurtboxes();
        InitializeCollisionCollider();
        InitializeHealth();
        resourceTable = new CharacterResourceTable(characterData.Resources);
        foreach (var resource in characterData.Resources)
        {
            resourceTable.AddResource(resource.Key, resource.Init);
        }

        if (GetComponentInChildren<RigController>() != null)
        {
            rigController = GetComponentInChildren<RigController>();
            rigController.SetUp(characterData.rigOffset, characterData.weightChangeSpeed);
        }

        EntityContainer.Instance.RegisterCharacter(this);
        isDie = false;
        hitStopTimer = 0f;
        IsHitStopped = false;

        foreach (var buffData in characterData.Buffs)
        {
            // 이 캐릭터를 Owner로 설정하여 BuffManager에 버프를 추가
            BuffManager.Instance.AddBuff(buffData, this);
        }

        if (AnimatorHasLayer(animator, 1))
        {
            animator.SetLayerWeight(1, 0);
        }

        // HP 및 Stamina 바 생성
        InitializeHPStaminaBar();
    }

    protected void InitializeHurtboxes()
    {
        foreach (var hurtboxData in characterData.Hurtboxes)
        {
            GameObject hurtboxObject = this.gameObject;
            Vector3 hurtboxPosition = transform.position + transform.forward * hurtboxData.Offset.x + transform.right * hurtboxData.Offset.y;

            SphereCollider hurtboxCollider = hurtboxObject.GetComponent<SphereCollider>();
            if (hurtboxCollider == null)
            {
                hurtboxCollider = hurtboxObject.AddComponent<SphereCollider>();
            }

            hurtboxCollider.isTrigger = true;
            hurtboxCollider.center = transform.InverseTransformPoint(hurtboxPosition);
            hurtboxCollider.radius = hurtboxData.Radius;
        }
    }

    protected void InitializeCollisionCollider()
    {
        collisionCollider = gameObject.AddComponent<SphereCollider>();
        collisionCollider.radius = characterData.colliderRadius;
        collisionCollider.isTrigger = false;
    }

    protected void InitializeHealth()
    {
        currentHealth = characterData.baseHP;
        maxHealth = characterData.baseHP;
    }



    protected virtual void Update()
    {
        if (!isServer && !isLocalPlayer)
            return; // 클라이언트에서만 동작이 실행되지 않도록 설정

        if (OnEvolution)
            return;

        if (IsHitStopped)
        {
            hitStopTimer -= Time.deltaTime;

            if (hitStopTimer <= 0f)
            {
                ResumeAfterHitStop();
            }
            // HitStop 중인 상태에서 Manual VFX를 멈추도록 설정
            foreach (var vfx in activeManualVfxList)
            {
                vfx.Stop(); // 모든 Manual Vfx 정지
            }
            return;
        }

        if (isDie)
            return;


        switch (currentState)
        {
            case CharacterState.Idle:
                HandleIdle();
                break;
            case CharacterState.Move:
                HandleMovement();
                break;
            case CharacterState.Action:
                HandleAction();
                break;
            case CharacterState.KnockBack:
                CmdHandleKnockback();
                break;
            case CharacterState.KnockBackSmash:
                CmdHandleKnockbackSmash();
                break;
        }

        if (Input.GetKeyDown(KeyCode.Keypad1))
        {
            if (isLocalPlayer)
            {
                CmdHandleEvolution(0);
            }
                 // 첫 번째 EvolutionInfo
        }
        else if (Input.GetKeyDown(KeyCode.Keypad2))
        {
            if (isLocalPlayer)
            {
                CmdHandleEvolution(1);
            }
        }
        else if (Input.GetKeyDown(KeyCode.Keypad3))
        {
            if (isLocalPlayer)
            {
                CmdHandleEvolution(2);
            }
        }

        if (AnimatorHasParameter(animator, "X"))
        {
            animator.SetFloat("X", Mathf.Lerp(animator.GetFloat("X"), 0, Time.deltaTime * 10f));
        }
        if (AnimatorHasParameter(animator, "Z"))
        {
            animator.SetFloat("Z", Mathf.Lerp(animator.GetFloat("Z"), 0, Time.deltaTime * 10f));
        }
    }

    public void ChangeStatePrev(CharacterState newState)
    {
        if (isServer)
        {
            ChangeState(newState);
            RpcChangeState(newState);
        }
        else
        {
            CmdChangeState(newState);
        }
    }

    private void ChangeState(CharacterState newState)
    {
        currentState = newState;
    }

    [Command]
    public void CmdChangeState(CharacterState newState)
    {
        ChangeState(newState);
        RpcChangeState(newState);
    }
    [ClientRpc]
    private void RpcChangeState(CharacterState newState)
    {
        if (!isServer)
        {
            ChangeState(newState);
        }
    }


    protected virtual void HandleIdle() { }

    protected virtual void HandleMovement() { } 

    protected void HandleAction()
    {
        if (currentActionData != null)
        {
            currentFrame += Time.deltaTime * 60f;

            if (currentFrame <= 1)
            {
                ApplyAutoCorrection(currentActionData); // 첫 프레임에 AutoCorrection 적용
            }

            float animationFrame = currentActionData.AnimationCurve.Evaluate(currentFrame);

            // 로컬 플레이어일 경우 애니메이션 재생 및 서버에 요청
            if (isLocalPlayer)
            {
                animator.Play(currentActionData.AnimationKey, 0, animationFrame / GetClipTotalFrames(currentActionData.AnimationKey));
                CmdPlayAnimation(currentActionData.AnimationKey, animationFrame / GetClipTotalFrames(currentActionData.AnimationKey));
            }

            // 특정 프레임에서 리소스를 소모
            foreach (var resourceUsage in currentActionData.Resources)
            {
                int resourceFrame = resourceUsage.Frame;
                if (Mathf.FloorToInt(currentFrame) == resourceFrame && !addedResourceFrames.Contains(resourceFrame))
                {
                    resourceTable.AddResource(resourceUsage.ResourceKey, resourceUsage.Count);
                    addedResourceFrames.Add(resourceFrame); // 처리된 프레임을 추가하여 중복 소모 방지
                    Debug.Log($"소모된 리소스: {resourceUsage.ResourceKey}, 남은 수량: {resourceTable.GetResourceValue(resourceUsage.ResourceKey)}");
                }
            }

            //SpecialMove
            foreach (var specialMovement in currentActionData.SpecialMovementList)
            {
                if (currentFrame >= specialMovement.StartFrame && currentFrame <= specialMovement.EndFrame)
                {
                    ApplySpecialMovement(specialMovement);
                    
                }
            }

            // TransformMove
            foreach (var movement in currentActionData.MovementList)
            {
                if (currentFrame >= movement.StartFrame && currentFrame <= movement.EndFrame)
                {
                    float t = (currentFrame - movement.StartFrame) / (float)(movement.EndFrame - movement.StartFrame);
                    Vector2 interpolatedSpeed = Vector2.Lerp(movement.StartValue, movement.EndValue, t);
                    Vector3 moveVector = transform.forward * interpolatedSpeed.y + transform.right * interpolatedSpeed.x;
                    Vector3 finalMoveVector = HandleCollisionAndSliding(moveVector.normalized, moveVector.magnitude);

                    // 로컬 플레이어의 이동을 서버에 동기화
                    if (isLocalPlayer)
                    {
                        transform.position += finalMoveVector;
                        CmdSyncMovement(finalMoveVector);
                    }
                }
            }

            foreach (var spawnData in currentActionData.ActionSpawnBulletList)
            {
                // 중복된 소환을 방지: HashSet에 포함되지 않은 경우에만 소환
                if (currentFrame >= spawnData.SpawnFrame && !spawnedBulletData.Contains(spawnData))
                {
                    Vector3 anchorPosition = GetAnchor(spawnData.Anchor);
                    Vector3 spawnPosition = anchorPosition + transform.forward * spawnData.Offset.y + transform.right * spawnData.Offset.x;

                    Vector3 spawnDirection;
                    switch (spawnData.Pivot)
                    {
                        case ActionSpawnBulletAnglePivot.Forward:
                            spawnDirection = Quaternion.Euler(0, spawnData.Angle, 0) * transform.forward;
                            break;

                        case ActionSpawnBulletAnglePivot.ToTarget:
                            if (target != null)
                            {
                                Vector3 directionToTarget = (target.transform.position - spawnPosition).normalized;
                                spawnDirection = Quaternion.Euler(0, spawnData.Angle, 0) * directionToTarget;
                            }
                            else
                            {
                                spawnDirection = Quaternion.Euler(0, spawnData.Angle, 0) * transform.forward;
                            }
                            break;

                        default:
                            spawnDirection = Quaternion.Euler(0, spawnData.Angle, 0) * transform.forward;
                            break;
                    }
                    // BulletData로부터 SerializedBulletData 생성
                    var bulletData = spawnData.BulletPrefab.bulletData;
                    SerializedBulletData serializedData = new SerializedBulletData(
                        bulletData.LiftTime,
                        bulletData.Speed,
                        bulletData.HitboxList,
                        bulletData.HitIdList
                    );

                    // 서버/클라이언트 구분하여 소환 로직 호출
                    //if (isServer)
                    //{
                    //    //ActionSpawnBullet(spawnPosition, spawnDirection, serializedData, spawnData.BulletPrefab.name);
                    //    //RpcActionSpawnBullet(spawnPosition, spawnDirection, serializedData, spawnData.BulletPrefab.name);
                    //}
                    //else
                        CmdActionSpawnBullet(spawnPosition, spawnDirection, serializedData, spawnData.BulletPrefab.name);


                    spawnedBulletData.Add(spawnData); // 중복 방지
                }
            }


            foreach (var vfxData in currentActionData.ActionSpawnVfxList)
            {
                if (currentFrame >= vfxData.SpawnFrame && !spawnedVfxData.Contains(vfxData))
                {
                    SpawnVfx(vfxData);
                    spawnedVfxData.Add(vfxData); // 중복 소환 방지용 추가
                }
            }

            // Manual VfxObject들의 시간 갱신
            foreach (var vfx in activeManualVfxList)
            {
                float vfxTime = (currentFrame - vfx.spawnFrame) / 60f;
                vfx.SetTime(vfxTime);
            }

            // Hitbox Cast
            foreach (var hitbox in currentActionData.HitboxList)
            {
                if (currentFrame >= hitbox.StartFrame && currentFrame <= hitbox.EndFrame)
                {
                    Vector3 hitboxPosition = transform.position + transform.right * hitbox.Offset.x + transform.forward * hitbox.Offset.y;
                    Collider[] hitColliders = Physics.OverlapSphere(hitboxPosition, hitbox.Radius);

                    foreach (var hitCollider in hitColliders)
                    {
                        CharacterBehaviour target = hitCollider.GetComponentInParent<CharacterBehaviour>();

                        if (target == null || target == this || target.isDie)
                        {
                            continue;
                        }

                        if (IsValidTarget(target))
                        {
                            if (hitTargets.ContainsKey(target) && hitTargets[target].Contains(hitbox.HitGroup))
                            {
                                continue;
                            }

                            // 히트를 로컬 플레이어에서 서버에 요청
                            if (isServer)
                            {
                                var hit = currentActionData.HitIdList.Find(x => x.HitId == hitbox.HitId).Clone();

                                hit.Attacker = this;
                                hit.Victim = target;
                                hit.Direction = (target.transform.position - transform.position).normalized;
                                hit.HitDamage *= characterData.baseATK;
                                HandleHit(hit);
                                RpcHandleHit(hit);
                            }
                            //else if (isLocalPlayer)
                            //{
                            //    CmdHandleHit(hitbox.HitId, target, currentActionData);
                            //}
                            if (!hitTargets.ContainsKey(target))
                            {
                                hitTargets[target] = new List<int>();
                            }
                            hitTargets[target].Add(hitbox.HitGroup);

                            break;
                        }
                    }
                }

                if (currentFrame >= currentActionData.ActionFrame)
                {
                    EndAction();
                }
            }
            if (currentFrame >= currentActionData.ActionFrame)
            {
                EndAction();
            }
        }
        else
        {
            Debug.LogWarning($"ActionData for {currentActionKey} not found in ActionTable.");
            EndAction();
        }
    }

    // 서버에 애니메이션 재생 요청
    [Command]
    private void CmdPlayAnimation(string animationKey, float normalizedTime)
    {
        RpcPlayAnimation(animationKey, normalizedTime);
    }

    // 클라이언트에 애니메이션 재생 동기화
    [ClientRpc]
    private void RpcPlayAnimation(string animationKey, float normalizedTime)
    {
        if (!isLocalPlayer)
        {
            animator.Play(animationKey, 0, normalizedTime);
        }
    }

    // 서버에 캐릭터 이동 동기화 요청
    [Command]
    private void CmdSyncMovement(Vector3 moveVector)
    {
        RpcSyncMovement(moveVector);
    }

    // 클라이언트에 캐릭터 이동 동기화
    [ClientRpc]
    private void RpcSyncMovement(Vector3 moveVector)
    {
        if (!isLocalPlayer)
        {
            transform.position += moveVector;
        }
    }


    protected void HandleHit(HitData hit)
    {

        // 서버에서 피해 적용 요청
        if (isServer)
        {
            OnHit(hit);
            RpcOnHit(hit);

            if (hit.Victim != null)
            {
                hit.Victim.TakeDamage(hit);
                hit.Victim.RpcTakeDamage(hit); ;
            }


            ApplyHitStop(hit.HitStopFrame);
            RpcApplyHitStop(hit.HitStopFrame);
            if (hit.Victim != null)
            {
                hit.Victim.ApplyHitStop(hit.HitStopFrame);
                hit.Victim.RpcApplyHitStop(hit.HitStopFrame);
            }
        }
        else
        {
            CmdApplyHitStop(hit.HitStopFrame);
        }

    }

    [Command]
    private void CmdHandleHit(HitData hit)
    {
        if (hit.Victim != null)
        {
            RpcHandleHit(hit);
            HandleHit(hit);
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

    private float GetClipTotalFrames(string animationKey)
    {
        var clipInfo = animator.GetCurrentAnimatorClipInfo(0);
        if (clipInfo.Length > 0)
        {
            var clip = clipInfo[0].clip;
            if (clip != null)
            {
                return clip.frameRate * clip.length;
            }
        }
        return 1f;
    }

    public void ApplyHitStop(float durationInFrames)
    {
        float durationInSeconds = durationInFrames / 60f;

        IsHitStopped = true;
        hitStopTimer = durationInSeconds;

        // 애니메이션 정지
        if (animator != null)
        {
            animator.speed = 0f;
        }

        // Manual VFX도 멈춤
        foreach (var vfx in activeManualVfxList)
        {
            vfx.Stop();
        }
    }

    [Command]
    void CmdApplyHitStop(float durationInFrames)
    {
        ApplyHitStop(durationInFrames);
        RpcApplyHitStop(durationInFrames);
    }

    [ClientRpc]
    public void RpcApplyHitStop(float durationInFrames)
    {
        if (!isServer)
        {
            ApplyHitStop(durationInFrames);
        }
    }

    protected void ResumeAfterHitStop()
    {
        IsHitStopped = false;

        // 애니메이션 재개
        if (animator != null)
        {
            animator.speed = 1f;
        }

        // Manual VFX 재개
        foreach (var vfx in activeManualVfxList)
        {
            float vfxTime = (currentFrame - vfx.spawnFrame) / 60f;
            vfx.SetTime(vfxTime); // 시간 재설정
            vfx.particle.Play();
        }
    }

   

    protected virtual void EndAction()
    {
        hitTargets.Clear();
        ChangeStatePrev(CharacterState.Idle);
        if (AnimatorHasLayer(animator, 1))
            animator.SetLayerWeight(1, 0);
        //currentFrame = 0;
        foreach (var vfx in activeManualVfxList)
        {
            vfx.OnDespawn();
        }
        activeManualVfxList.Clear();
    }


    private void ApplySpecialMovement(SpecialMovementData specialMovementData)
    {
        switch (specialMovementData.MoveType)
        {
            case SpecialMovementType.AddInput:
                inputDirection = CalculateInputDirection();

                if (inputDirection != Vector3.zero)
                {
                    currentSpeed = characterData.moveSpeed * inputDirection.magnitude;
                    moveVector = HandleCollisionAndSliding(inputDirection.normalized, currentSpeed) * specialMovementData.Value;
                    transform.position += moveVector;

                    // 회전 처리
                    if (specialMovementData.CanRotate)
                    {
                        targetRotation = Quaternion.LookRotation(inputDirection);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
                    }

                    // 애니메이터에 방향 인풋 신호 전달
                    Vector3 localInputDirection = transform.InverseTransformDirection(inputDirection);
                    localInputDirection = localInputDirection.normalized;
                    if (AnimatorHasParameter(animator, "X"))
                    {
                        animator.SetFloat("X", localInputDirection.x, 0.05f, Time.deltaTime);
                    }
                    if (AnimatorHasParameter(animator, "Z"))
                    {
                        animator.SetFloat("Z", localInputDirection.z, 0.05f, Time.deltaTime);
                    }
                    if (AnimatorHasLayer(animator, 1))
                    {
                        // 현재 가중치를 Animator에서 가져옴
                        float currentLayerWeight = animator.GetLayerWeight(1);

                        // 목표 가중치 설정
                        float targetLayerWeight = 1f; // 활성화하려면 1f로 변경

                        // 가중치를 부드럽게 변화시킴
                        float newLayerWeight = Mathf.Lerp(currentLayerWeight, targetLayerWeight, Time.deltaTime * 5f);

                        // Animator에 새로운 가중치 설정
                        animator.SetLayerWeight(1, newLayerWeight);
                    }

                }
                else
                {
                    // 인풋이 없는 경우 속도를 줄임
                    currentSpeed = Mathf.Lerp(currentSpeed, 0, Time.deltaTime);
                    moveVector = HandleCollisionAndSliding(Vector3.zero, currentSpeed) * specialMovementData.Value;
                    transform.position += moveVector;

                    // 애니메이터 속도 감소
                    if (AnimatorHasParameter(animator, "X"))
                    {
                        animator.SetFloat("X", Mathf.Lerp(animator.GetFloat("X"), 0, Time.deltaTime * 10f));
                    }
                    if (AnimatorHasParameter(animator, "Z"))
                    {
                        animator.SetFloat("Z", Mathf.Lerp(animator.GetFloat("Z"), 0, Time.deltaTime * 10f));
                    }
                    if (AnimatorHasLayer(animator, 1))
                    {
                        // 현재 가중치를 Animator에서 가져옴
                        float currentLayerWeight = animator.GetLayerWeight(1);

                        // 목표 가중치 설정
                        float targetLayerWeight = 0f; // 활성화하려면 1f로 변경

                        // 가중치를 부드럽게 변화시킴
                        float newLayerWeight = Mathf.Lerp(currentLayerWeight, targetLayerWeight, Time.deltaTime * 5f);

                        // Animator에 새로운 가중치 설정
                        animator.SetLayerWeight(1, newLayerWeight);
                    }
                }

                
                break;

            case SpecialMovementType.LookRotateTarget:
                if (target != null)
                {
                    Vector3 directionToTarget = (target.transform.position - transform.position).normalized;
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,specialMovementData.Value * Time.deltaTime);
                }
                break;

            default:
                Debug.LogWarning("처리되지 않은 SpecialMovementType입니다.");
                break;
        }
    }

    private Vector3 CalculateInputDirection()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0))
        {
            initialTouchPosition = Input.mousePosition;
            touchStartTime = Time.time;
            touchDelta = Vector3.zero;
            //touchDeltaDistance = touchDelta.magnitude;
            //touchElapsedTime = Time.time - touchStartTime;
        }
        else if (Input.GetMouseButton(0))
        {
            touchDelta = Input.mousePosition - initialTouchPosition;
            touchDeltaDistance = touchDelta.magnitude;
            touchElapsedTime = Time.time - touchStartTime;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            touchDelta = Vector3.zero;
            touchDeltaDistance = touchDelta.magnitude;
            touchElapsedTime = Time.time - touchStartTime;
        }
        else
        {
            touchDelta = Vector3.zero;
            touchDeltaDistance = touchDelta.magnitude;
            touchElapsedTime = Time.time - touchStartTime;
        }
        return GetDirectionalInput(touchDelta);
#endif

#if UNITY_IOS || UNITY_ANDROID
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0); // 첫 번째 터치 입력만 처리
            if (touch.phase == TouchPhase.Began)
            {
                initialTouchPosition = touch.position;
                touchStartTime = Time.time;
                touchDelta = Vector3.zero;
            }
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                touchDelta = (Vector3)touch.position - initialTouchPosition;
                touchDeltaDistance = touchDelta.magnitude;
                touchElapsedTime = Time.time - touchStartTime;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                touchDelta = Vector3.zero;
                touchDeltaDistance = touchDelta.magnitude;
                touchElapsedTime = Time.time - touchStartTime;
            }
            else
            {
                touchDelta = Vector3.zero;
                touchDeltaDistance = touchDelta.magnitude;
                touchElapsedTime = Time.time - touchStartTime;
            }
            return GetDirectionalInput(touchDelta);
        }
#endif

        return Vector3.zero;
    }

    private Vector3 GetDirectionalInput(Vector3 touchDelta)
    {
        touchDeltaDistance = touchDelta.magnitude;
        speedMultiplier = Mathf.Clamp(touchDeltaDistance / ResourceHolder.Instance.gameVariables.maxDistance, 0f, 1f);

        inputDirection = new Vector3(touchDelta.x, 0, touchDelta.y).normalized;

        Vector3 cameraForward = Camera.main.transform.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();

        Vector3 cameraRight = Camera.main.transform.right;
        cameraRight.y = 0;
        cameraRight.Normalize();

        return (inputDirection.x * cameraRight + inputDirection.z * cameraForward) * speedMultiplier;
    }

    private void ActionSpawnBullet(Vector3 position, Vector3 direction, SerializedBulletData serializedData, string prefabName)
    {
        GameObject bulletPrefab = NetworkManager.singleton.spawnPrefabs.Find(prefab => prefab.name == prefabName);
        if (bulletPrefab == null)
        {
            Debug.LogError($"Prefab '{prefabName}' not found in RegisteredSpawnablePrefabs.");
            return;
        }

        // 서버에서 Bullet 생성
        GameObject bulletObject = Instantiate(bulletPrefab, position, Quaternion.LookRotation(direction));

        NetworkServer.Spawn(bulletObject);
        // Bullet 초기화
        BulletBehaviour bullet = bulletObject.GetComponent<BulletBehaviour>();
        if (bullet != null)
        {
            if (isServer)
            {
                bullet.Initialize(this, direction, serializedData);
                bullet.RpcInitialize(this.GetComponent<NetworkIdentity>(), direction, serializedData);
            }
            else
                bullet.CmdInitialize(this.GetComponent<NetworkIdentity>(), direction, serializedData);
        }
    }
    [Command]
    public void CmdActionSpawnBullet(Vector3 position, Vector3 direction, SerializedBulletData serializedData, string prefabName)
    {
        ActionSpawnBullet( position,  direction,  serializedData,  prefabName);
        //RpcActionSpawnBullet(position, direction, serializedData, prefabName);
    }


    [ClientRpc]
    private void RpcActionSpawnBullet(Vector3 position, Vector3 direction, SerializedBulletData serializedData, string prefabName)
    {
        if (isServer) return; // 서버에서는 이미 처리되었으므로 클라이언트만 실행
        ActionSpawnBullet(position, direction, serializedData, prefabName);
    }

    public virtual void TakeDamage(HitData hit)
    {
        currentHealth -= Mathf.Clamp(hit.HitDamage,0, hit.HitDamage);

        // 공격자가 있다면 그 방향을 바라보게 함
        if (hit.Attacker != null)
        {
            Vector3 directionToAttacker = (hit.Attacker.transform.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(directionToAttacker);
            transform.rotation = lookRotation; // 즉시 회전
        }
        OnAttacked(hit.Attacker);

        // 데미지 텍스트 표시
        if (ResourceHolder.Instance != null && ResourceHolder.Instance.gameVariables != null && hit.HitDamage > 0)
        {
            DamageTextManager.Instance.ShowDamageText(transform.position, (int)hit.HitDamage);
        }


        if (currentHealth <= 0 || isDie)
        {
            currentHealth = 0;
            Die();
            return;
        }
        else
        {
            if (hit.HitType == HitType.DamageOnly)
            {
                return;
            }

            if (currentState == CharacterState.Action)
                EndAction();

            switch (hit.HitType)
            {
                case HitType.Weak:
                    if (isServer)
                    {
                        StartKnockBack(hit);
                        RpcStartKnockBack(hit);
                    }
                    else
                    {
                        CmdStartKnockBack(hit);
                    }
                    break;

                case HitType.Strong:
                    if (isServer)
                    {
                        StartKnockBackSmash(hit);
                        RpcStartKnockBackSmash(hit);
                    }
                    else
                    {
                        CmdStartKnockBackSmash(hit);
                    }
                    break;
            }
        }

        
    }

    [Command]
    public void CmdTakeDamage(Vector3 hitDirection, HitData hit)
    {
        TakeDamage(hit);
        RpcTakeDamage(hit);
    }

    [ClientRpc]
    public void RpcTakeDamage(HitData hit)
    {
        if (!isServer)
        {
            if (isLocalPlayer)
            {
                return;
            }

            if (hit.Attacker != null)
            {
                TakeDamage(hit);
            }
        }
    }

    public void Heal(float value)
    {
        currentHealth += Mathf.Clamp(value, currentHealth, maxHealth); //FixMe
    }

    public void HealPercent(float value)
    {
        currentHealth += Mathf.Clamp(currentHealth*(1 + (value) / 100), currentHealth, maxHealth); //FixMe
    }
    
    public void SetHp(float value)
    {
        currentHealth += Mathf.Clamp(value, 0, maxHealth);
    }

    [HideInInspector] public bool hit;
    [HideInInspector] public CharacterBehaviour currentHitTarget;
    [HideInInspector] public CharacterBehaviour lastHitTarget;
    [HideInInspector] public bool attacked;
    [HideInInspector] public CharacterBehaviour currentAttacker;
    [HideInInspector] public CharacterBehaviour lastAttacker;
    public void OnHit(HitData hit)
    {
        currentHitTarget = target;
        lastHitTarget = target;

        if (hit.HitApplyOwnerResource)
            resourceTable.AddResource(hit.ResourceKey, hit.Value);

        //foreach (var buff in hit.HitApplyBuffs)
        //{
        //    BuffManager.Instance.AddBuff(buff, hit.Victim);
        //}

        BuffManager.Instance.TriggerBuffEffect(BuffTriggerType.OwnerHit, hit);
        BuffManager.Instance.TriggerBuffEffect(BuffTriggerType.EveryHit, hit);

        StartCoroutine(ResetHitRecentlyFlag());
    }

    [Command]
    public void CmdOnHit(HitData hit)
    {
        // 서버에서 처리 후 클라이언트에 동기화
        OnHit(hit);
        RpcOnHit(hit);
    }

    [ClientRpc]
    public void RpcOnHit(HitData hit)
    {
        if (!isServer)
        {
            // 다른 클라이언트에서 처리
            OnHit(hit);
        }
    }

    public void OnAttacked(CharacterBehaviour attacker)
    {
        // 로컬에서 호출 시 서버에 동기화 요청
        if (isLocalPlayer)
        {
            CmdOnAttacked(attacker);
        }
        else
        {
            // 로컬에서 처리
            ProcessOnAttacked(attacker);
        }
    }

    [Command]
    private void CmdOnAttacked(CharacterBehaviour attacker)
    {
        // 서버에서 처리 후 클라이언트에 동기화
        RpcOnAttacked(attacker);
        ProcessOnAttacked(attacker); // 서버에서 로컬 처리
    }

    [ClientRpc]
    private void RpcOnAttacked(CharacterBehaviour attacker)
    {
        if (!isLocalPlayer)
        {
            // 다른 클라이언트에서 처리
            ProcessOnAttacked(attacker);
        }
    }

    private void ProcessOnAttacked(CharacterBehaviour attacker)
    {
        attacked = true;
        currentAttacker = attacker;
        lastAttacker = attacker;
        StartCoroutine(ResetAttackedRecentlyFlag());
    }

    private IEnumerator ResetHitRecentlyFlag()
    {
        yield return null; // 1 프레임 대기
        hit = false;
        currentHitTarget = null;
    }

    private IEnumerator ResetAttackedRecentlyFlag()
    {
        yield return null; // 1 프레임 대기
        attacked = false;
        currentAttacker = null;
    }

    private void StartKnockBack(HitData hit)
    {
        ChangeStatePrev(CharacterState.KnockBack);
        knockBackDirection = hit.Direction;
        initialKnockBackSpeed = hit.KnockbackPower;

        // HitStun을 프레임 단위로 계산
        knockBackDuration = Mathf.Max(hit.HitStunFrame / 60f, initialKnockBackSpeed / 10f);
        knockBackTimer = 0f;

        animator.Play("Knockback", 0, 0f);
    }

    [Command]
    public void CmdStartKnockBack(HitData hit)
    {
        StartKnockBack(hit); // 서버에서 로직 실행
        RpcStartKnockBack(hit); // 클라이언트에 동기화
    }

    [ClientRpc]
    private void RpcStartKnockBack(HitData hit)
    {
        if (!isServer)
        {
            StartKnockBack(hit); // 클라이언트에서 로컬 동작 실행
        }
    }

    private void StartKnockBackSmash(HitData hit)
    {
        ChangeStatePrev(CharacterState.KnockBackSmash);
        //currentState = CharacterState.KnockBackSmash; // 로컬에서 상태 변경
        knockBackDirection = hit.Direction.normalized;
        initialKnockBackSpeed = hit.KnockbackPower;

        // HitStun을 프레임 단위로 계산
        knockBackDuration = Mathf.Max(hit.HitStunFrame / 60f, initialKnockBackSpeed / 10f);
        knockBackTimer = 0f;

        animator.Play("Knockback", 0, 0f);
    }

    [Command]
    public void CmdStartKnockBackSmash(HitData hit)
    {
        StartKnockBackSmash(hit); // 서버에서 로직 실행
        RpcStartKnockBackSmash(hit); // 클라이언트에 동기화
    }

    [ClientRpc]
    private void RpcStartKnockBackSmash(HitData hit)
    {
        if (!isServer)
        {
            StartKnockBackSmash(hit);
        }

    }

    private float initialBurstDuration = 0.2f; // 초기 강한 넉백 구간
    private float flightDuration = 0.3f;  // 일정한 속도로 날아가는 시간 (감속 포함)
    private float decelerationDuration = 0.3f;  // 감속 시간
    private float totalDuration => initialBurstDuration + flightDuration + decelerationDuration; // 총 넉백 지속 시간

    public bool IsHitStopped { get => isHitStopped; set => isHitStopped = value; }

    protected virtual void HandleKnockback()
    {
        knockBackTimer += Time.deltaTime;

        float speedModifier;

        if (knockBackTimer <= initialBurstDuration)
        {
            float t = knockBackTimer / initialBurstDuration;
            speedModifier = Mathf.Lerp(1f, 0.3f, t);
        }
        else if (knockBackTimer <= initialBurstDuration + flightDuration)
        {
            float t = (knockBackTimer - initialBurstDuration) / flightDuration;
            speedModifier = Mathf.Lerp(0.3f, 0.15f, t);
        }
        else
        {
            float t = (knockBackTimer - initialBurstDuration - flightDuration) / decelerationDuration;
            speedModifier = Mathf.Lerp(0.15f, 0f, t);
        }

        currentknockBackSpeed = initialKnockBackSpeed * speedModifier;

        Vector3 knockBackMovement = HandleCollisionAndSliding(knockBackDirection, currentknockBackSpeed);
        transform.position += knockBackMovement;

        if (knockBackTimer >= totalDuration || currentknockBackSpeed < 0.01f)
        {
            ChangeStatePrev(CharacterState.Idle);
            initialKnockBackSpeed = 0f;
            currentknockBackSpeed = 0f;
        }
    }

    [ClientRpc]
    void RpcHandleKnockback()
    {
        if (!isServer)
        {
            HandleKnockback();
        }
    }

    [Command]
    void CmdHandleKnockback()
    {
        HandleKnockback();
        RpcHandleKnockback();
    }

    protected virtual void HandleKnockbackSmash()
    {
        knockBackTimer += Time.deltaTime;

        float speedModifier;

        if (knockBackTimer <= initialBurstDuration)
        {
            float t = knockBackTimer / initialBurstDuration;
            speedModifier = Mathf.Lerp(1f, 0.5f, t);
        }
        else if (knockBackTimer <= initialBurstDuration + flightDuration)
        {
            float t = (knockBackTimer - initialBurstDuration) / flightDuration;
            speedModifier = Mathf.Lerp(0.5f, 0.3f, t);
        }
        else
        {
            float t = (knockBackTimer - initialBurstDuration - flightDuration) / decelerationDuration;
            speedModifier = Mathf.Lerp(0.3f, 0f, t);
        }
        currentknockBackSpeed = initialKnockBackSpeed * speedModifier;
        Vector3 knockBackMovement = knockBackDirection * currentknockBackSpeed * Time.deltaTime;


        if (currentknockBackSpeed < ResourceHolder.Instance.gameVariables.knockBackPowerReference)
        {
            ChangeStatePrev(CharacterState.KnockBack);
            knockBackDuration = Mathf.Max(knockBackTimer, currentknockBackSpeed / 10f);
            return;
        }

        if (knockBackTimer >= totalDuration || currentknockBackSpeed < 0.001f)
        {
            ChangeStatePrev(CharacterState.Idle);
            initialKnockBackSpeed = 0f;
            currentknockBackSpeed = 0f;
            return;
        }


        //벽충돌
        RaycastHit hit;
        var radius = characterData.colliderRadius;
        LayerMask wallLayer = LayerMask.GetMask("WallCollider");
        float knockbackPowerForWallCollide = Mathf.Max(initialKnockBackSpeed * ResourceHolder.Instance.gameVariables.collideReduceMultiplier, 5f);
        if (Physics.SphereCast(transform.position, radius, knockBackDirection, out hit, knockBackMovement.magnitude, wallLayer))
        {
            HitData wallHitData = new HitData
            {
                Attacker = null, // 벽은 attacker가 아님
                Victim = this, // 
                Direction = Vector3.Reflect(knockBackDirection, hit.normal).normalized,
                KnockbackPower = currentknockBackSpeed * ResourceHolder.Instance.gameVariables.collideReduceMultiplier, // 속도 감소
                HitType = HitType.Strong, // 속도 감소
                HitDamage = 0f, // 벽 충돌은 대미지 없음
                HitStopFrame = knockbackPowerForWallCollide,
            };

            if (isServer)
            {
                HandleHit(wallHitData);
                RpcHandleHit(wallHitData);
            }
        }
        else
        {
            transform.position += knockBackMovement;
        }


        // 다른 CharacterBehaviour와의 충돌 처리
        Collider[] colliders = Physics.OverlapSphere(transform.position, radius);
        foreach (var collider in colliders)
        {
            CharacterBehaviour otherCharacter = collider.GetComponentInParent<CharacterBehaviour>();

            // 자신이 아니고, 상대가 KnockBackSmash 상태가 아닐 경우만 처리
            if (otherCharacter != null && otherCharacter != this && (otherCharacter.teamType is not TeamType.Player && teamType is not TeamType.Player) &&
                (otherCharacter.currentState != CharacterState.KnockBackSmash || otherCharacter.currentState != CharacterState.KnockBack) &&
                knockBackTimer > ResourceHolder.Instance.gameVariables.collideAvoidTime)
            {
                // 진행 방향과의 각도 확인
                Vector3 toOtherCharacter = (otherCharacter.transform.position - transform.position).normalized;
                float angleToOther = Vector3.Angle(knockBackDirection, toOtherCharacter);

                if (angleToOther > ResourceHolder.Instance.gameVariables.maxCollisionAngle) // 설정된 각도를 초과하면 무시
                    continue;

                Vector3 collisionNormal = toOtherCharacter;

                // 위치 보정
                float overlapDistance = radius * 2f - Vector3.Distance(transform.position, otherCharacter.transform.position);
                transform.position -= collisionNormal * (overlapDistance / 2f);
                otherCharacter.transform.position += collisionNormal * (overlapDistance / 2f);

                // 충돌에 따른 새로운 HitData 생성
                float knockbackPowerForThis = currentknockBackSpeed * ResourceHolder.Instance.gameVariables.collideReduceMultiplier;
                float knockbackPowerForOther = currentknockBackSpeed * ResourceHolder.Instance.gameVariables.collideReduceMultiplier;

                HitData newHitDataForThis = new HitData
                {
                    Attacker = otherCharacter,
                    Victim = this,
                    HitDamage = 0f,
                    Direction = -collisionNormal,
                    HitStopFrame = ResourceHolder.Instance.gameVariables.hitstopWhenCollide,
                    HitType = HitType.Strong,
                    KnockbackPower = knockbackPowerForThis
                };

                HitData newHitDataForOther = new HitData
                {
                    Attacker = this,
                    Victim = otherCharacter,
                    HitDamage = 0f,
                    Direction = collisionNormal,
                    HitStopFrame = ResourceHolder.Instance.gameVariables.hitstopWhenCollide,
                    HitType = HitType.Strong,
                    KnockbackPower = knockbackPowerForOther
                };

                // 넉백 및 HitStop 처리
                if (isServer)
                {
                    HandleHit(newHitDataForThis);
                    HandleHit(newHitDataForOther);
                    RpcHandleHit(newHitDataForThis);
                    RpcHandleHit(newHitDataForOther);
                }
            }
        }
    }
    [ClientRpc]
    void RpcHandleKnockbackSmash()
    {
        if (!isServer)
        {
            HandleKnockback();
        }
    }

    [Command]
    void CmdHandleKnockbackSmash()
    {
        HandleKnockbackSmash();
        RpcHandleKnockbackSmash();
    }

    protected virtual void Die()
    {
        isDie = true;
        animator.Play("Die");
        if (EntityContainer.InstanceExist)
        {
            EntityContainer.Instance.UnregisterCharacter(this);
        }

        var despawnDelay = 2f;

        if (hpStaminaBarController != null)
            hpStaminaBarController.Despawn(despawnDelay);
        Destroy(gameObject, despawnDelay);
    }

    private bool CheckActionConditions(ActionConditionData condition)
    {
        switch (condition.Type)
        {
            case ActionConditionType.HasResource:
                return resourceTable.HasResource(condition.ResourceKey, condition.Count);

            // 추후 추가할 조건 유형에 대한 로직을 여기에 추가합니다.

            default:
                Debug.LogWarning($"알 수 없는 조건 타입: {condition.Type}");
                return false;
        }
    }

    public void StartAction(ActionKey actionKey)
    {
        var actionsForKey = characterData.ActionTable
        .Where(entry => entry.ActionKey == actionKey)
        .Select(entry => entry.ActionData)
        .ToList();

        foreach (var originActionData in actionsForKey)
        {

            var actionData = originActionData.Clone();
            // 조건을 체크하여 실행할 수 있는지 확인
            bool canExecute = true;
            foreach (var condition in actionData.Conditions)
            {
                if (!CheckActionConditions(condition))
                {
                    int currentResourceCount = resourceTable.GetResourceValue(condition.ResourceKey);
                    Debug.Log($"조건 미충족으로 액션 실행 불가: {actionKey}. 요구 리소스: {condition.ResourceKey}, 요구 수량: {condition.Count}, 현재 수량: {currentResourceCount}");
                    canExecute = false;
                    break;
                }
            }


            // 실행 가능 여부에 따라 액션 실행 또는 다음 액션으로 이동
            if (canExecute)
            {
                Debug.Log($"Starting action: {actionKey}");

                // 조건을 만족하는 ActionData를 실행하도록 설정
                currentActionKey = actionKey;
                currentActionData = actionData;
                ChangeStatePrev(CharacterState.Action);
                currentFrame = 0;
                addedResourceFrames.Clear();
                spawnedVfxData.Clear();
                spawnedBulletData.Clear();
                if (AnimatorHasLayer(animator, 1))
                {
                    animator.SetLayerWeight(1, 0);
                    animator.SetFloat("X", 0);
                    animator.SetFloat("Z", 0);
                }
                hitTargets.Clear();

                BuffManager.Instance.TriggerBuffEffect(BuffTriggerType.OwnerAction, actionData);

                // 루프를 종료하여 첫 번째로 조건을 만족하는 액션만 실행
                return;
            }
        }

        // 모든 액션을 검사했지만 실행 가능한 액션이 없는 경우
        Debug.Log($"No valid action found for {actionKey} after checking all conditions.");
    }

    [ClientRpc]
    public void RpcStartAction(ActionKey actionKey)
    {
        if (!isServer)
        {
            StartAction(actionKey);
        }
    }
    
    [Command]
    public void CmdStartAction(ActionKey actionKey)
    {
        StartAction(actionKey);
        RpcStartAction(actionKey);
    }

    protected bool IsCollisionDetected(Vector3 targetPosition, float radius, out Vector3 collisionNormal, out RaycastHit[] hits)
    {
        hits = Physics.SphereCastAll(targetPosition, radius, direction, radius, LayerMask.GetMask("WallCollider"));
        if (hits.Length > 0)
        {
            collisionNormal = hits[0].normal;
            return true;
        }
        collisionNormal = Vector3.zero;
        return false;
    }

    protected Vector3 HandleCollisionAndSliding(Vector3 moveDirection, float moveSpeed)
    {
        RaycastHit hit;
        var radius = characterData.colliderRadius;
        LayerMask wallLayer = LayerMask.GetMask("WallCollider");
        LayerMask characterLayer = LayerMask.GetMask("Character");

        Vector3 finalDirection = moveDirection;
        float finalSpeedAdjustment = 1f;

        // 벽 충돌 처리
        if (Physics.SphereCast(transform.position, radius, finalDirection, out hit, moveSpeed * Time.deltaTime, wallLayer))
        {
            Vector3 normal = hit.normal;
            Vector3 slideDirection = Vector3.ProjectOnPlane(finalDirection, normal).normalized;

            float firstAngle = Vector3.Angle(finalDirection, normal);
            float firstSpeedAdjustment = Mathf.Clamp01(1 - Mathf.Abs(firstAngle - 90) / 90f);

            // 두 번째 벽 충돌 검사
            if (Physics.SphereCast(transform.position, radius, slideDirection, out hit, moveSpeed * Time.deltaTime, wallLayer))
            {
                Vector3 secondNormal = hit.normal;
                slideDirection = Vector3.ProjectOnPlane(slideDirection, secondNormal).normalized;

                float secondAngle = Vector3.Angle(slideDirection, secondNormal);
                float secondSpeedAdjustment = Mathf.Clamp01(1 - Mathf.Abs(secondAngle - 90) / 90f);

                firstSpeedAdjustment *= secondSpeedAdjustment;
            }

            finalDirection = slideDirection;
            finalSpeedAdjustment *= firstSpeedAdjustment;
        }

        // 캐릭터 충돌 처리 (이동 방향에 따라 판단)
        Collider[] characterColliders = Physics.OverlapSphere(transform.position, radius, characterLayer);
        foreach (var collider in characterColliders)
        {
            CharacterBehaviour otherCharacter = collider.GetComponentInParent<CharacterBehaviour>();

            // 자신이 아니고 KnockBack 상태가 아닌 캐릭터만 처리
            if (otherCharacter != null && otherCharacter != this &&
                otherCharacter.currentState != CharacterState.KnockBack &&
                otherCharacter.currentState != CharacterState.KnockBackSmash)
            {
                Vector3 toOtherCharacter = (otherCharacter.transform.position - transform.position).normalized;

                // 이동 방향과 충돌 방향이 일정 각도 이내일 경우만 충돌 처리
                float angleToOther = Vector3.Angle(finalDirection, toOtherCharacter);
                if (angleToOther > 90f) // 이동 방향과 반대면 충돌 무시
                    continue;

                Vector3 characterNormal = toOtherCharacter;
                Vector3 characterSlideDirection = Vector3.ProjectOnPlane(finalDirection, characterNormal).normalized;

                float characterAngle = Vector3.Angle(finalDirection, characterNormal);
                float characterSpeedAdjustment = Mathf.Clamp01(1 - Mathf.Abs(characterAngle - 90) / 90f);

                // 충돌 결과 반영
                finalDirection = characterSlideDirection;
                finalSpeedAdjustment *= characterSpeedAdjustment;
            }
        }

        // 최종 이동 방향 및 속도 계산
        return finalDirection * moveSpeed * finalSpeedAdjustment * Time.deltaTime;
    }

    ///
    ///
    ///
    public bool OnEvolution;

    [Command]
    private void CmdHandleEvolution(int index)
    {
        HandleEvolution(index); // Host에서 실행
        RpcHandleEvolution(connectionToClient.connectionId, index); // 클라이언트에 동기화
    }

    [ClientRpc]
    private void RpcHandleEvolution(int connectionId, int index)
    {
        if (connectionToClient == null || connectionToClient.connectionId != connectionId)
        {
            Debug.LogWarning("이 RPC는 다른 클라이언트를 대상으로 하므로 무시합니다.");
            return;
        }

        if (characterData == null || characterData.EvolutionInfos == null || characterData.EvolutionInfos.Count <= index)
        {
            Debug.LogError($"클라이언트에서 Evolution 데이터가 유효하지 않습니다. Index: {index}");
            return;
        }

        HandleEvolution(index);
    }

    private void HandleEvolution(int index)
    {
        if (characterData == null || characterData.EvolutionInfos.Count <= index)
        {
            Debug.LogWarning($"진화 데이터가 유효하지 않음. Index: {index}");
            return;
        }

        var evolutionInfo = characterData.EvolutionInfos[index];
        if (evolutionInfo?.NextCharacter == null)
        {
            Debug.LogError($"EvolutionInfo 또는 NextCharacter가 null입니다. Index: {index}");
            return;
        }

        if (OnEvolution) return; // 중복 처리 방지

        if (AnimatorHasLayer(animator, 1))
        {
            animator.SetLayerWeight(1, 0);
            animator.SetFloat("X", 0);
            animator.SetFloat("Z", 0);
        }
        // 3. 진화 전 애니메이션 재생 및 딜레이
        PlayEvolutionStartAnimation();
        if (isClient)
            RpcPlayEvolutionStart(connectionToClient.connectionId);
        OnEvolution = true;

        StartCoroutine(HandleEvolutionCoroutine(index, evolutionInfo));
    }

    [ClientRpc]
    private void RpcPlayEvolutionStart(int connectionId)
    {
        // 특정 클라이언트에서만 실행
        if (connectionToClient == null || connectionToClient.connectionId != connectionId)
        {
            return;
        }

        PlayEvolutionStartAnimation();
    }

    private IEnumerator HandleEvolutionCoroutine(int index, EvolutionInfo evolutionInfo)
    {
        // 진화 전 애니메이션 대기 (1.5초)
        yield return new WaitForSeconds(1.5f);

        // 4. 기존 캐릭터의 정보 저장
        Vector3 currentPosition = transform.position;
        Quaternion currentRotation = transform.rotation;

        // 5. 기존 캐릭터의 네트워크 연결 정보 저장
        var oldConnection = connectionToClient;

        // 새 캐릭터 생성
        var nextCharacterPrefab = evolutionInfo.NextCharacter.gameObject;
        var newCharacterInstance = Instantiate(nextCharacterPrefab, currentPosition, currentRotation);
        var newCharacterBehaviour = newCharacterInstance.GetComponent<CharacterBehaviour>();

        if (newCharacterBehaviour == null)
        {
            Debug.LogError("새 캐릭터 프리팹에 CharacterBehaviour가 없습니다.");
            OnEvolution = false; // 상태 해제
            yield break;
        }

        // 네트워크 동기화
        EntityContainer.Instance.UnregisterCharacter(this);
        EntityContainer.Instance.RegisterCharacter(newCharacterBehaviour);

        // 8. 네트워크 상에서 새 객체를 먼저 생성
        // 네트워크 객체 관리
        if (isServer)
        {
            NetworkServer.Spawn(newCharacterInstance);
            NetworkServer.ReplacePlayerForConnection(oldConnection, newCharacterInstance);
        }
        // HpStaminaBar 제거 동기화
        RpcHandleHpStaminaBarDespawn();

        // 새로운 캐릭터 초기화
        newCharacterBehaviour.SetLocalPlayer();
        newCharacterBehaviour.StartCoroutine(newCharacterBehaviour.HandleEvolutionComplete());

        // 기존 객체 제거
        StartCoroutine(DelayedDestroy());


        Debug.Log($"로컬 플레이어가 {newCharacterBehaviour.name}(으)로 진화했습니다.");
    }

    private IEnumerator DelayedDestroy()
    {
        yield return new WaitForEndOfFrame();
        if (isServer)
        {
            NetworkServer.Destroy(gameObject);
        }
    }


    private void PlayEvolutionStartAnimation()
    {
        if (animator != null)
        {
            animator.Play("EvolutionStart");
            Debug.Log("진화 시작 애니메이션 재생");
        }
        else
        {
            Debug.LogWarning("Animator가 존재하지 않아 진화 시작 애니메이션을 재생할 수 없습니다.");
        }
    }

    [ClientRpc]
    private void RpcHandleHpStaminaBarDespawn()
    {
        hpStaminaBarController?.Despawn(0f);
    }


    [ClientRpc]
    public void RpcPlayEvolutionComplete()
    {
        if (!isLocalPlayer) return;

        StartCoroutine(HandleEvolutionComplete());
    }

    public IEnumerator HandleEvolutionComplete()
    {
        yield return new WaitForEndOfFrame();
        OnEvolution = true;
        PlayEvolutionCompleteAnimation();

        // 진화 완료 대기 (0.5초)
        yield return new WaitForSeconds(1f);

        // 진화 완료
        OnEvolution = false;
        Debug.Log($"진화 완료: {name}");
    }

    private void PlayEvolutionCompleteAnimation()
    {
        if (animator != null)
        {
            animator.Play("EvolutionComplete");
            Debug.Log("진화 완료 애니메이션 재생");
        }
        else
        {
            Debug.LogWarning("Animator가 존재하지 않아 진화 완료 애니메이션을 재생할 수 없습니다.");
        }
    }
    [Client]
    private void SetLocalPlayer()
    {
        Debug.Log($"로컬 플레이어가 {name}로 전환되었습니다.");
        // 새로운 캐릭터에서 로컬 플레이어 처리
        if (isLocalPlayer)
        {
            Camera.main.GetComponent<CameraController>().SetTarget(transform);
        }
    }


    /// <summary>
    /// 오토코렉션
    /// </summary>
    protected virtual void ApplyAutoCorrection(ActionData actionData)
    {
        switch (actionData.AutoCorrection.correctionType)
        {
            case AutoCorrectionType.None:
                // 아무 동작도 하지 않음
                break;

            case AutoCorrectionType.Target:
                if (target != null)
                {
                    LookAtTarget(target.transform.position);
                }
                break;

            case AutoCorrectionType.Entity:
                var entity = EntityContainer.Instance.CharacterList
                    .FirstOrDefault(e => e.name.Contains(actionData.AutoCorrection.entityName));
                if (entity != null)
                {
                    LookAtTarget(entity.transform.position);
                }
                break;

            case AutoCorrectionType.Character:
                var character = EntityContainer.Instance.CharacterList
                    .FirstOrDefault(c => c.name.Contains(actionData.AutoCorrection.characterName));
                if (character != null)
                {
                    LookAtTarget(character.transform.position);
                }
                break;
        }
    }

    private void LookAtTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = targetRotation;
    }

    /// <summary>
    /// 인풋 메시지
    /// </summary>

    public void ReceiveInputMessage(InputMessage message)
    {
        HandleInputMessage(message);
    }

    public virtual void HandleInputMessage(InputMessage message)
    {

        switch (currentState)
        {
            case CharacterState.Idle:
            case CharacterState.Move:
                ProcessInputMessage(message);
                break;
            case CharacterState.Action:
                break;
        }
    }

    public void ProcessInputMessage(InputMessage message)
    {
        switch (message)
        {
            case InputMessage.A:
                if (isServer)
                {
                    // 서버에서는 직접 abc() 실행 및 모든 클라이언트에 동기화
                    StartAction(ActionKey.Basic01);
                    RpcStartAction(ActionKey.Basic01);
                }
                else if (isLocalPlayer)
                {
                    // 클라이언트는 서버에 요청
                    CmdStartAction(ActionKey.Basic01);
                }
                break;
            case InputMessage.B:
                if (isServer)
                {
                    // 서버에서는 직접 abc() 실행 및 모든 클라이언트에 동기화
                    StartAction(ActionKey.Special01);
                    RpcStartAction(ActionKey.Special01);
                }
                else if (isLocalPlayer)
                {
                    // 클라이언트는 서버에 요청
                    CmdStartAction(ActionKey.Special01);
                }
                break;
            case InputMessage.C:
                if (isServer)
                {
                    // 서버에서는 직접 abc() 실행 및 모든 클라이언트에 동기화
                    StartAction(ActionKey.Special02);
                    RpcStartAction(ActionKey.Special02);
                }
                else if (isLocalPlayer)
                {
                    // 클라이언트는 서버에 요청
                    CmdStartAction(ActionKey.Special02);
                }
                break;
            default:
                Debug.LogWarning("처리되지 않은 InputMessage: " + message);
                break;
        }
    }

    /// <summary>
    /// 기즈모 드로잉
    /// </summary>
    private void OnDrawGizmos()
    {
        if (characterData != null)
        {
            Gizmos.color = new Color(0f, 0f, 1f, 0.5f);
            foreach (var hurtbox in characterData.Hurtboxes)
            {
                Vector3 hurtboxPosition = transform.position + transform.forward * hurtbox.Offset.y + transform.right * hurtbox.Offset.x;
                Gizmos.DrawSphere(hurtboxPosition, hurtbox.Radius);
            }
        }

        if (currentState == CharacterState.Action && characterData != null)
        {
            if (characterData.TryGetActionData(currentActionKey, out ActionData currentActionData))
            {
                foreach (var hitbox in currentActionData.HitboxList)
                {
                    if (currentFrame >= hitbox.StartFrame && currentFrame <= hitbox.EndFrame)
                    {
                        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
                        Vector3 hitboxPosition = transform.position + transform.forward * hitbox.Offset.y + transform.right * hitbox.Offset.x;
                        Gizmos.DrawSphere(hitboxPosition, hitbox.Radius);
                    }
                }
            }
        }

        // 타겟 인디케이터 그리기
        if (target != null)
        {
            Gizmos.color = this is PlayerController ? Color.red : Color.blue;
            Gizmos.DrawSphere(target.transform.position + Vector3.up * 2f, 0.25f);
        }
    }
    ///vfx 관련 시각 처리 manual
    ///
    private struct ManualVfxInfo
    {
        public VfxObject Obj;
        public float StartFrame;
        public float EndFrame;
    }

    //animator 패러미터 검사
    public bool AnimatorHasParameter(Animator animator, string paramName)
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName)
            {
                return true;
            }
        }
        return false;
    }
    public bool AnimatorHasLayer(Animator animator, int layerIndex)
    {
        return layerIndex >= 0 && layerIndex < animator.layerCount;
    }

    // Action에서 무언가를 소환하는 메서드
    private void SpawnVfx(ActionSpawnVfxData vfxData)
    {
        Vector3 spawnPosition = transform.position + transform.forward * vfxData.Offset.y + transform.right * vfxData.Offset.x;
        Quaternion spawnRotation = Quaternion.Euler(0, vfxData.Angle, 0);

        VfxObject vfx = Instantiate(vfxData.VfxPrefab, spawnPosition, spawnRotation);
        NetworkServer.Spawn(vfx.gameObject);
        vfx.SetTransform(transform, spawnPosition, Quaternion.Euler(0, vfxData.Angle, 0), Vector3.one);
        vfx.OnSpawn(currentFrame);

        if (vfx.PlayType == VfxPlayType.Manual)
        {
            activeManualVfxList.Add(vfx); // Manual 타입만 관리 리스트에 추가하여 Hitstop에 영향 받게 설정
        }
    }
    private Vector3 GetAnchor(SpawnAnchor anchorType)
    {
        switch (anchorType)
        {
            case SpawnAnchor.ThisCharacter:
                return transform.position;
            case SpawnAnchor.Target:
                return target != null ? target.transform.position : transform.position;
            default:
                return Vector3.zero;
        }
    }

    private void InitializeHPStaminaBar()
    {
        if (ResourceHolder.Instance.gameVariables.HPStaminaBarPrefab != null)
        {
            hpStaminaBarInstance = Instantiate(ResourceHolder.Instance.gameVariables.HPStaminaBarPrefab, transform.position, Quaternion.identity);
            hpStaminaBarInstance.transform.SetParent(EntityContainer.Instance.transform);
        }
        // 초기 위치 설정 (캐릭터 머리 위)
        hpStaminaBarController = hpStaminaBarInstance.GetComponentInChildren<HpStaminaBarController>();
        hpStaminaBarController.target = this;
    }
}
