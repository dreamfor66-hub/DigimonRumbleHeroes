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
    public CharacterState currentState = CharacterState.Idle; // SyncVar�� �����Ͽ� ����ȭ
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

    // Knockback ���� ������
    [SyncVar]
    private Vector3 knockBackDirection;
    [SyncVar]
    private float knockBackSpeed;
    [SyncVar]
    private float knockBackDuration;
    [SyncVar]
    private float knockBackTimer;

    // HitStop ���� ������
    [SyncVar]
    private float hitStopTimer; // HitStop �ð��� �����ϴ� Ÿ�̸�
    [SyncVar]
    private bool isHitStopped; // HitStop ���¸� ��Ÿ���� ����

    // target ���� ����
    [SyncVar]
    public CharacterBehaviour target;
    private GameObject targetIndicator;

    // ActionMove�� ����, player���� �ַ� ����� �������
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
            return; // Ŭ���̾�Ʈ������ ������ ������� �ʵ��� ����

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
                ApplyAutoCorrection(currentActionData); // ù �����ӿ� AutoCorrection ����
            }

            float animationFrame = currentActionData.AnimationCurve.Evaluate(currentFrame);

            // ���� �÷��̾��� ��� �ִϸ��̼� ��� �� ������ ��û
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

                    // ���� �÷��̾��� �̵��� ������ ����ȭ
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

                    // Bullet �ν��Ͻ� ���� �� �ʱ�ȭ
                    BulletBehaviour bullet = Instantiate(spawnData.BulletPrefab, spawnPosition, Quaternion.LookRotation(spawnDirection));
                    bullet.Initialize(this, spawnDirection); // Bullet�� �ʱ� ���� ����
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

                            // ��Ʈ�� ���� �÷��̾�� ������ ��û
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

    // ������ �ִϸ��̼� ��� ��û
    [Command]
    private void CmdPlayAnimation(string animationKey, float normalizedTime)
    {
        RpcPlayAnimation(animationKey, normalizedTime);
    }

    // Ŭ���̾�Ʈ�� �ִϸ��̼� ��� ����ȭ
    [ClientRpc]
    private void RpcPlayAnimation(string animationKey, float normalizedTime)
    {
        if (!isLocalPlayer)
        {
            animator.Play(animationKey, 0, normalizedTime);
        }
    }

    // ������ ĳ���� �̵� ����ȭ ��û
    [Command]
    private void CmdSyncMovement(Vector3 moveVector)
    {
        RpcSyncMovement(moveVector);
    }

    // Ŭ���̾�Ʈ�� ĳ���� �̵� ����ȭ
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
            // ������ ���� ���� ��û
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
            // HitStop ����
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

        // �ִϸ��̼� ����
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

        // �ִϸ��̼� �簳
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

                    // ȸ�� ó��
                    if (specialMovementData.CanRotate)
                    {
                        targetRotation = Quaternion.LookRotation(inputDirection);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
                    }
                }
                else
                {
                    // ��ǲ�� ���� ��� �ӵ��� ����
                    currentSpeed = Mathf.Lerp(currentSpeed, 0, Time.deltaTime); // ���� �ӵ� ���� ����
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
                Debug.LogWarning("ó������ ���� SpecialMovementType�Դϴ�.");
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
            Touch touch = Input.GetTouch(0); // ù ��° ��ġ �Է¸� ó��
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
        Debug.Log("����");
        currentHealth -= damage;

        // �����ڰ� �ִٸ� �� ������ �ٶ󺸰� ��
        if (attacker != null)
        {
            Vector3 directionToAttacker = (attacker.transform.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(directionToAttacker);
            transform.rotation = lookRotation; // ��� ȸ��
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
        // �������� ó�� �� Ŭ���̾�Ʈ�� ����ȭ
        OnHit(target); // �������� ���� ó��
        RpcOnHit(target);
    }

    [ClientRpc]
    private void RpcOnHit(CharacterBehaviour target)
    {
        if (!isServer)
        {
            // �ٸ� Ŭ���̾�Ʈ���� ó��
            OnHit(target);
        }
    }

    public void OnAttacked(CharacterBehaviour attacker)
    {
        // ���ÿ��� ȣ�� �� ������ ����ȭ ��û
        if (isLocalPlayer)
        {
            CmdOnAttacked(attacker);
        }
        else
        {
            // ���ÿ��� ó��
            ProcessOnAttacked(attacker);
        }
    }

    [Command]
    private void CmdOnAttacked(CharacterBehaviour attacker)
    {
        // �������� ó�� �� Ŭ���̾�Ʈ�� ����ȭ
        RpcOnAttacked(attacker);
        ProcessOnAttacked(attacker); // �������� ���� ó��
    }

    [ClientRpc]
    private void RpcOnAttacked(CharacterBehaviour attacker)
    {
        if (!isLocalPlayer)
        {
            // �ٸ� Ŭ���̾�Ʈ���� ó��
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
        yield return null; // 1 ������ ���
        hit = false;
        currentHitTarget = null;
    }

    private IEnumerator ResetAttackedRecentlyFlag()
    {
        yield return null; // 1 ������ ���
        attacked = false;
        currentAttacker = null;
    }

    private void StartKnockBack(Vector3 hitDirection, HitData hitData)
    {
        ChangeStatePrev(CharacterState.KnockBack);
        knockBackDirection = hitDirection.normalized;
        knockBackSpeed = hitData.KnockbackPower;

        // HitStun�� ������ ������ ���
        knockBackDuration = Mathf.Max(hitData.HitStunFrame / 60f, knockBackSpeed / 10f);
        knockBackTimer = 0f;

        animator.Play("Knockback");
    }

    [Command]
    public void CmdStartKnockBack(Vector3 hitDirection, HitData hitData)
    {
        StartKnockBack(hitDirection, hitData); // �������� ���� ����
        RpcStartKnockBack(hitDirection, hitData); // Ŭ���̾�Ʈ�� ����ȭ
    }

    [ClientRpc]
    private void RpcStartKnockBack(Vector3 hitDirection, HitData hitData)
    {
        if (!isServer)
        {
            StartKnockBack(hitDirection, hitData); // Ŭ���̾�Ʈ���� ���� ���� ����
        }
    }

    private void StartKnockBackSmash(Vector3 hitDirection, HitData hitData)
    {
        ChangeStatePrev(CharacterState.KnockBackSmash);
        currentState = CharacterState.KnockBackSmash; // ���ÿ��� ���� ����
        knockBackDirection = hitDirection.normalized;
        knockBackSpeed = hitData.KnockbackPower;

        // HitStun�� ������ ������ ���
        knockBackDuration = Mathf.Max(hitData.HitStunFrame / 60f, knockBackSpeed / 10f);
        knockBackTimer = 0f;

        animator.Play("Knockback");
    }

    [Command]
    public void CmdStartKnockBackSmash(Vector3 hitDirection, HitData hitData)
    {
        StartKnockBackSmash(hitDirection, hitData); // �������� ���� ����
        RpcStartKnockBackSmash(hitDirection, hitData); // Ŭ���̾�Ʈ�� ����ȭ
    }

    [ClientRpc]
    private void RpcStartKnockBackSmash(Vector3 hitDirection, HitData hitData)
    {
        if (!isServer)
        {
            StartKnockBackSmash(hitDirection, hitData);
        }

    }

    private float initialBurstDuration = 0.2f; // �ʱ� ���� �˹� ����
    private float flightDuration = 0.3f;  // ������ �ӵ��� ���ư��� �ð� (���� ����)
    private float decelerationDuration = 0.3f;  // ���� �ð�
    private float totalDuration => initialBurstDuration + flightDuration + decelerationDuration; // �� �˹� ���� �ð�

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
            // ���� �浹���� �� HitStop ����
            ApplyHitStop(5.5f);  // ����ڿ��Ը� HitStop 5������ ����

            // �ݻ� �� �ӵ� ����
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

        // ù ��° �浹�� �����ϴ� SphereCast
        if (Physics.SphereCast(transform.position, radius, moveDirection, out hit, moveSpeed * Time.deltaTime, wallLayer))
        {
            Vector3 normal = hit.normal;
            Vector3 slideDirection = Vector3.ProjectOnPlane(moveDirection, normal).normalized;

            float firstAngle = Vector3.Angle(moveDirection, normal);
            float firstSpeedAdjustment = Mathf.Clamp01(1 - Mathf.Abs(firstAngle - 90) / 90f);

            // ������ �������� �� ��° �浹�� �˻��ϴ� SphereCast
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
    /// �����ڷ���
    /// </summary>
    protected virtual void ApplyAutoCorrection(ActionData actionData)
    {
        switch (actionData.AutoCorrection.correctionType)
        {
            case AutoCorrectionType.None:
                // �ƹ� ���۵� ���� ����
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
    /// ��ǲ �޽���
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
                    // ���������� ���� abc() ���� �� ��� Ŭ���̾�Ʈ�� ����ȭ
                    StartAction(ActionKey.Basic01);
                    RpcStartAction(ActionKey.Basic01);
                }
                else if (isLocalPlayer)
                {
                    // Ŭ���̾�Ʈ�� ������ ��û
                    CmdStartAction(ActionKey.Basic01);
                }
                break;
            case InputMessage.B:
                if (isServer)
                {
                    // ���������� ���� abc() ���� �� ��� Ŭ���̾�Ʈ�� ����ȭ
                    StartAction(ActionKey.Special01);
                    RpcStartAction(ActionKey.Special01);
                }
                else if (isLocalPlayer)
                {
                    // Ŭ���̾�Ʈ�� ������ ��û
                    CmdStartAction(ActionKey.Special01);
                }
                break;
            case InputMessage.C:
                if (isServer)
                {
                    // ���������� ���� abc() ���� �� ��� Ŭ���̾�Ʈ�� ����ȭ
                    StartAction(ActionKey.Special02);
                    RpcStartAction(ActionKey.Special02);
                }
                else if (isLocalPlayer)
                {
                    // Ŭ���̾�Ʈ�� ������ ��û
                    CmdStartAction(ActionKey.Special02);
                }
                break;
            default:
                Debug.LogWarning("ó������ ���� InputMessage: " + message);
                break;
        }
    }

    /// <summary>
    /// ����� �����
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

        // Ÿ�� �ε������� �׸���
        if (target != null)
        {
            Gizmos.color = this is PlayerController ? Color.red : Color.blue;
            Gizmos.DrawSphere(target.transform.position + Vector3.up * 2f, 0.25f);
        }
    }
}
