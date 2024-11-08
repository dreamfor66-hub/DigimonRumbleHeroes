using Mirror;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    public CharacterState currentState = CharacterState.Idle; // SyncVar로 변경하여 동기화
    [SyncVar]
    protected ActionKey currentActionKey;

    [SyncVar]
    protected float currentFrame;
    [SyncVar]
    public bool isDie;

    [SerializeField,Sirenix.OdinInspector.ReadOnly, SyncVar]
    protected float currentHealth;

    protected Dictionary<CharacterBehaviour, List<int>> hitTargets;

    public SphereCollider collisionCollider;

    // Knockback 관련 변수들
    [SyncVar]
    private Vector3 knockBackDirection;
    [SyncVar]
    private float knockBackSpeed;
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


    protected virtual void Start()
    {
        animator = GetComponentInChildren<Animator>();
        currentSpeed = characterData.moveSpeed;
        hitTargets = new Dictionary<CharacterBehaviour, List<int>>();
        InitializeHurtboxes();
        InitializeCollisionCollider();
        InitializeHealth();
        EntityContainer.Instance.RegisterCharacter(this);
        isDie = false;
        hitStopTimer = 0f;
        isHitStopped = false;
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
    }

    protected virtual void Update()
    {
        if (!isServer && !isLocalPlayer)
            return; // 클라이언트에서만 동작이 실행되지 않도록 설정

        if (isHitStopped)
        {
            hitStopTimer -= Time.deltaTime;
            if (hitStopTimer <= 0f)
            {
                ResumeAfterHitStop();
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
        if (characterData.TryGetActionData(currentActionKey, out ActionData currentActionData))
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
                    Vector3 moveVector = new Vector3(interpolatedSpeed.x, 0, interpolatedSpeed.y);
                    Vector3 finalMoveVector = HandleCollisionAndSliding(moveVector.normalized, moveVector.magnitude);

                    // 로컬 플레이어의 이동을 서버에 동기화
                    if (isLocalPlayer)
                    {
                        CmdSyncMovement(finalMoveVector);
                    }
                }
            }

            foreach (var spawnData in currentActionData.ActionSpawnBulletList)
            {
                if (Mathf.RoundToInt(currentFrame) == spawnData.SpawnFrame)
                {
                    Vector3 spawnPosition = transform.position + transform.forward * spawnData.Offset.y + transform.right * spawnData.Offset.x;

                    Vector3 spawnDirection;
                    switch (spawnData.Pivot)
                    {
                        case ActionSpawnBulletAnglePivot.Forward:
                            spawnDirection = Quaternion.Euler(0, spawnData.Angle, 0) * transform.forward;
                            break;

                        case ActionSpawnBulletAnglePivot.ToTarget:
                            if (target != null)
                            {
                                Vector3 directionToTarget = (target.transform.position - transform.position).normalized;
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

                    // Bullet 인스턴스 생성 및 초기화
                    BulletBehaviour bullet = Instantiate(spawnData.BulletPrefab, spawnPosition, Quaternion.LookRotation(spawnDirection));
                    bullet.Initialize(this, spawnDirection); // Bullet의 초기 방향 설정
                }
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

                        if (target == null || target == this)
                        {
                            continue;
                        }

                        bool isValidTarget = (this is PlayerController && target is EnemyController) ||
                                             (this is EnemyController && target is PlayerController);

                        if (isValidTarget)
                        {
                            if (hitTargets.ContainsKey(target) && hitTargets[target].Contains(hitbox.HitGroup))
                            {
                                continue;
                            }

                            // 히트를 로컬 플레이어에서 서버에 요청
                            if (isServer)
                            {
                                HandleHit(hitbox.HitId, target, currentActionData);
                                RpcHandleHit(hitbox.HitId, target, currentActionData);
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

    protected void HandleHit(int hitId, CharacterBehaviour target, ActionData currentActionData)
    {
        var hitData = currentActionData.HitIdList.Find(hit => hit.HitId == hitId);

        if (hitData != null)
        {
            Vector3 hitDirection = (target.transform.position - transform.position).normalized;
            // 서버에 피해 적용 요청
            if (isServer)
            {
                target.TakeDamage(hitData.HitDamage, hitDirection, hitData, this);
                target.RpcTakeDamage(hitData.HitDamage, hitDirection, hitData, this);
            }
            //else
            //{
            //    target.CmdTakeDamage(hitData.HitDamage, hitDirection, hitData, this);
            //}

            if (isServer)
            {
                OnHit(target);
                RpcOnHit(target);
            }
            //else
            //{
            //    CmdOnHit(target);
            //}
            float hitStopDuration = hitData.HitStopFrame;
            // HitStop 적용
            if (isServer)
            {
                ApplyHitStop(hitStopDuration);
                RpcApplyHitStop(hitStopDuration);
                target.ApplyHitStop(hitStopDuration);
                target.RpcApplyHitStop(hitStopDuration);
            }

            else
            {
                CmdApplyHitStop(hitStopDuration);
            }
            
            
            

            Debug.Log($"Hit {target.name} for {hitData.HitDamage} damage with {hitData.HitStopFrame} frames of hitstop.");
        }
    }


    [Command]
    private void CmdHandleHit(int hitId, CharacterBehaviour target, ActionData currentActionData)
    {
        if (target != null)
        {
            RpcHandleHit(hitId, target, currentActionData);
            HandleHit(hitId, target, currentActionData);
        }
    }
    [ClientRpc]
    private void RpcHandleHit(int hitId, CharacterBehaviour target, ActionData currentActionData)
    {
        if (!isServer && !isLocalPlayer)
        {
            if (target != null)
            {
                HandleHit(hitId, target, currentActionData);
            }
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

        isHitStopped = true;
        hitStopTimer = durationInSeconds;

        // 애니메이션 정지
        if (animator != null)
        {
            animator.speed = 0f;
        }
    }

    [Command]
    void CmdApplyHitStop(float durationInFrames)
    {
        ApplyHitStop(durationInFrames);
        RpcApplyHitStop(durationInFrames);
    }

    [ClientRpc]
    void RpcApplyHitStop(float durationInFrames)
    {
        if (!isServer)
        {
            ApplyHitStop(durationInFrames);
        }
    }

    protected void ResumeAfterHitStop()
    {
        isHitStopped = false;

        // 애니메이션 재개
        if (animator != null)
        {
            animator.speed = 1f;
        }
    }

   

    protected virtual void EndAction()
    {
        hitTargets.Clear();
        ChangeStatePrev(CharacterState.Idle);
        currentFrame = 0;
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
                    moveVector = HandleCollisionAndSliding(inputDirection.normalized, currentSpeed);
                    transform.position += moveVector;

                    // 회전 처리
                    if (specialMovementData.CanRotate)
                    {
                        targetRotation = Quaternion.LookRotation(inputDirection);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
                    }
                }
                else
                {
                    // 인풋이 없는 경우 속도를 줄임
                    currentSpeed = Mathf.Lerp(currentSpeed, 0, Time.deltaTime); // 감속 속도 조절 가능
                    moveVector = HandleCollisionAndSliding(Vector3.zero, currentSpeed);
                    transform.position += moveVector;
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


    public virtual void TakeDamage(float damage, Vector3 hitDirection, HitData hitData, CharacterBehaviour attacker)
    {
        Debug.Log("여기");
        currentHealth -= damage;

        // 공격자가 있다면 그 방향을 바라보게 함
        if (attacker != null)
        {
            Vector3 directionToAttacker = (attacker.transform.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(directionToAttacker);
            transform.rotation = lookRotation; // 즉시 회전
        }

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        else
        {
            if (hitData.hitType == HitType.DamageOnly)
            {
                return;
            }

            switch (hitData.hitType)
            {
                case HitType.Weak:
                    if (isServer)
                    {
                        StartKnockBack(hitDirection, hitData);
                        RpcStartKnockBack(hitDirection, hitData);
                    }
                    else
                    {
                        CmdStartKnockBack(hitDirection, hitData);
                    }
                    break;

                case HitType.Strong:
                    if (isServer)
                    {
                        StartKnockBackSmash(hitDirection, hitData);
                        RpcStartKnockBackSmash(hitDirection, hitData);
                    }
                    else
                    {
                        CmdStartKnockBackSmash(hitDirection, hitData);
                    }
                    break;
            }
        }

        OnAttacked(attacker);
    }

    [Command]
    public void CmdTakeDamage(float damage, Vector3 hitDirection, HitData hitData, CharacterBehaviour attacker)
    {
        TakeDamage(damage, hitDirection, hitData, attacker);
        RpcTakeDamage(damage, hitDirection, hitData, attacker);
    }

    [ClientRpc]
    public void RpcTakeDamage(float damage, Vector3 hitDirection, HitData hitData, CharacterBehaviour attacker)
    {
        if (!isServer)
        {
            if (isLocalPlayer)
            {
                return;
            }

            if (attacker != null)
            {
                TakeDamage(damage, hitDirection, hitData, attacker);
            }
        }
    }

    [HideInInspector] public bool hit;
    [HideInInspector] public CharacterBehaviour currentHitTarget;
    [HideInInspector] public CharacterBehaviour lastHitTarget;
    [HideInInspector] public bool attacked;
    [HideInInspector] public CharacterBehaviour currentAttacker;
    [HideInInspector] public CharacterBehaviour lastAttacker;
    public void OnHit(CharacterBehaviour target)
    {
        hit = true;
        currentHitTarget = target;
        lastHitTarget = target;
        StartCoroutine(ResetHitRecentlyFlag());
    }

    [Command]
    private void CmdOnHit(CharacterBehaviour target)
    {
        // 서버에서 처리 후 클라이언트에 동기화
        OnHit(target); // 서버에서 로컬 처리
        RpcOnHit(target);
    }

    [ClientRpc]
    private void RpcOnHit(CharacterBehaviour target)
    {
        if (!isServer)
        {
            // 다른 클라이언트에서 처리
            OnHit(target);
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

    private void StartKnockBack(Vector3 hitDirection, HitData hitData)
    {
        ChangeStatePrev(CharacterState.KnockBack);
        knockBackDirection = hitDirection.normalized;
        knockBackSpeed = hitData.KnockbackPower;

        // HitStun을 프레임 단위로 계산
        knockBackDuration = Mathf.Max(hitData.HitStunFrame / 60f, knockBackSpeed / 10f);
        knockBackTimer = 0f;

        animator.Play("Knockback");
    }

    [Command]
    public void CmdStartKnockBack(Vector3 hitDirection, HitData hitData)
    {
        StartKnockBack(hitDirection, hitData); // 서버에서 로직 실행
        RpcStartKnockBack(hitDirection, hitData); // 클라이언트에 동기화
    }

    [ClientRpc]
    private void RpcStartKnockBack(Vector3 hitDirection, HitData hitData)
    {
        if (!isServer)
        {
            StartKnockBack(hitDirection, hitData); // 클라이언트에서 로컬 동작 실행
        }
    }

    private void StartKnockBackSmash(Vector3 hitDirection, HitData hitData)
    {
        ChangeStatePrev(CharacterState.KnockBackSmash);
        currentState = CharacterState.KnockBackSmash; // 로컬에서 상태 변경
        knockBackDirection = hitDirection.normalized;
        knockBackSpeed = hitData.KnockbackPower;

        // HitStun을 프레임 단위로 계산
        knockBackDuration = Mathf.Max(hitData.HitStunFrame / 60f, knockBackSpeed / 10f);
        knockBackTimer = 0f;

        animator.Play("Knockback");
    }

    [Command]
    public void CmdStartKnockBackSmash(Vector3 hitDirection, HitData hitData)
    {
        StartKnockBackSmash(hitDirection, hitData); // 서버에서 로직 실행
        RpcStartKnockBackSmash(hitDirection, hitData); // 클라이언트에 동기화
    }

    [ClientRpc]
    private void RpcStartKnockBackSmash(Vector3 hitDirection, HitData hitData)
    {
        if (!isServer)
        {
            StartKnockBackSmash(hitDirection, hitData);
        }

    }

    private float initialBurstDuration = 0.2f; // 초기 강한 넉백 구간
    private float flightDuration = 0.3f;  // 일정한 속도로 날아가는 시간 (감속 포함)
    private float decelerationDuration = 0.3f;  // 감속 시간
    private float totalDuration => initialBurstDuration + flightDuration + decelerationDuration; // 총 넉백 지속 시간

    protected virtual void HandleKnockback()
    {
        knockBackTimer += Time.deltaTime;

        float speedModifier;

        if (knockBackTimer <= initialBurstDuration)
        {
            float t = knockBackTimer / initialBurstDuration;
            speedModifier = Mathf.Lerp(2f, 1f, t);
        }
        else if (knockBackTimer <= initialBurstDuration + flightDuration)
        {
            float t = (knockBackTimer - initialBurstDuration) / flightDuration;
            speedModifier = Mathf.Lerp(1f, 0.5f, t);
        }
        else
        {
            float t = (knockBackTimer - initialBurstDuration - flightDuration) / decelerationDuration;
            speedModifier = Mathf.Lerp(0.5f, 0f, t);
        }

        Vector3 knockBackMovement = HandleCollisionAndSliding(knockBackDirection, knockBackSpeed * speedModifier);
        transform.position += knockBackMovement;

        if (knockBackTimer >= totalDuration || knockBackSpeed < 0.1f)
        {
            ChangeStatePrev(CharacterState.Idle);
            knockBackSpeed = 0f;
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
            speedModifier = Mathf.Lerp(2f, 1f, t);
        }
        else if (knockBackTimer <= initialBurstDuration + flightDuration)
        {
            float t = (knockBackTimer - initialBurstDuration) / flightDuration;
            speedModifier = Mathf.Lerp(1f, 0.5f, t);
        }
        else
        {
            float t = (knockBackTimer - initialBurstDuration - flightDuration) / decelerationDuration;
            speedModifier = Mathf.Lerp(0.5f, 0f, t);
        }

        Vector3 knockBackMovement = knockBackDirection * knockBackSpeed * speedModifier * Time.deltaTime;

        RaycastHit hit;
        var radius = characterData.colliderRadius;
        LayerMask wallLayer = LayerMask.GetMask("WallCollider");

        if (Physics.SphereCast(transform.position, radius, knockBackDirection, out hit, knockBackMovement.magnitude, wallLayer))
        {
            // 벽에 충돌했을 때 HitStop 적용
            ApplyHitStop(5.5f);  // 대상자에게만 HitStop 5프레임 적용

            // 반사 시 속도 감소
            Vector3 collisionNormal = hit.normal;
            Vector3 reflectDirection = Vector3.Reflect(knockBackDirection, collisionNormal).normalized;
            knockBackDirection = reflectDirection;
            knockBackSpeed *= 0.5f;
        }
        else
        {
            transform.position += knockBackMovement;
        }

        if (knockBackSpeed < 7f)
        {
            ChangeStatePrev(CharacterState.KnockBack);
            knockBackDuration = Mathf.Max(knockBackTimer, knockBackSpeed / 10f);
        }

        if (knockBackTimer >= totalDuration || knockBackSpeed < 0.1f)
        {
            ChangeStatePrev(CharacterState.Idle);
            knockBackSpeed = 0f;
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
        Destroy(gameObject, 2f);
    }

    public void StartAction(ActionKey actionKey)
    {

        Debug.Log($"Starting action: {actionKey}");
        currentActionKey = actionKey;
        ChangeStatePrev(CharacterState.Action);
        currentFrame = 0;

        hitTargets.Clear();

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

        // 첫 번째 충돌을 감지하는 SphereCast
        if (Physics.SphereCast(transform.position, radius, moveDirection, out hit, moveSpeed * Time.deltaTime, wallLayer))
        {
            Vector3 normal = hit.normal;
            Vector3 slideDirection = Vector3.ProjectOnPlane(moveDirection, normal).normalized;

            float firstAngle = Vector3.Angle(moveDirection, normal);
            float firstSpeedAdjustment = Mathf.Clamp01(1 - Mathf.Abs(firstAngle - 90) / 90f);

            // 수정된 방향으로 두 번째 충돌을 검사하는 SphereCast
            if (Physics.SphereCast(transform.position, radius, slideDirection, out hit, moveSpeed * Time.deltaTime, wallLayer))
            {
                Vector3 secondNormal = hit.normal;
                slideDirection = Vector3.ProjectOnPlane(slideDirection, secondNormal).normalized;

                float secondAngle = Vector3.Angle(slideDirection, secondNormal);
                float secondSpeedAdjustment = Mathf.Clamp01(1 - Mathf.Abs(secondAngle - 90) / 90f);

                firstSpeedAdjustment *= secondSpeedAdjustment;
            }

            return slideDirection * moveSpeed * firstSpeedAdjustment * Time.deltaTime;
        }
        else
        {
            return moveDirection * moveSpeed * Time.deltaTime;
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

    protected virtual void HandleInputMessage(InputMessage message)
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
}
