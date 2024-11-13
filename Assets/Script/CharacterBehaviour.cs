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
    public CharacterState currentState = CharacterState.Idle; // SyncVar�� �����Ͽ� ����ȭ
    [SyncVar]
    protected ActionKey currentActionKey;
    public ActionData currentActionData;

    [SyncVar]
    protected float currentFrame;
    [SyncVar]
    public bool isDie;

    [SerializeField,Sirenix.OdinInspector.ReadOnly, SyncVar]
    public float currentHealth;

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
    private RigController rigController;

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


    //vfx ���� ����
    // Manual Vfx ������ ����Ʈ
    private List<VfxObject> activeManualVfxList = new List<VfxObject>();
    private HashSet<ActionSpawnVfxData> spawnedVfxData = new HashSet<ActionSpawnVfxData>();
    private HashSet<ActionSpawnBulletData> spawnedBulletData = new HashSet<ActionSpawnBulletData>();
    private HashSet<int> addedResourceFrames = new HashSet<int>();


    // hpBar UI
    private GameObject hpStaminaBarInstance; // ������ HP Bar �ν��Ͻ�
    protected HpStaminaBarController hpStaminaBarController;

    //resource ���� ����
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
            // �� ĳ���͸� Owner�� �����Ͽ� BuffManager�� ������ �߰�
            BuffManager.Instance.AddBuff(buffData, this);
        }

        if (AnimatorHasLayer(animator, 1))
        {
            animator.SetLayerWeight(1, 0);
        }

        // HP �� Stamina �� ����
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
    }

    protected virtual void Update()
    {
        if (!isServer && !isLocalPlayer)
            return; // Ŭ���̾�Ʈ������ ������ ������� �ʵ��� ����

        if (IsHitStopped)
        {
            hitStopTimer -= Time.deltaTime;
            if (hitStopTimer <= 0f)
            {
                ResumeAfterHitStop();
            }

            // HitStop ���� ���¿��� Manual VFX�� ���ߵ��� ����
            foreach (var vfx in activeManualVfxList)
            {
                vfx.Stop(); // ��� Manual Vfx ����
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
                ApplyAutoCorrection(currentActionData); // ù �����ӿ� AutoCorrection ����
            }

            float animationFrame = currentActionData.AnimationCurve.Evaluate(currentFrame);

            // ���� �÷��̾��� ��� �ִϸ��̼� ��� �� ������ ��û
            if (isLocalPlayer)
            {
                animator.Play(currentActionData.AnimationKey, 0, animationFrame / GetClipTotalFrames(currentActionData.AnimationKey));
                CmdPlayAnimation(currentActionData.AnimationKey, animationFrame / GetClipTotalFrames(currentActionData.AnimationKey));
            }

            // Ư�� �����ӿ��� ���ҽ��� �Ҹ�
            foreach (var resourceUsage in currentActionData.Resources)
            {
                int resourceFrame = resourceUsage.Frame;
                if (Mathf.FloorToInt(currentFrame) == resourceFrame && !addedResourceFrames.Contains(resourceFrame))
                {
                    resourceTable.AddResource(resourceUsage.ResourceKey, resourceUsage.Count);
                    addedResourceFrames.Add(resourceFrame); // ó���� �������� �߰��Ͽ� �ߺ� �Ҹ� ����
                    Debug.Log($"�Ҹ�� ���ҽ�: {resourceUsage.ResourceKey}, ���� ����: {resourceTable.GetResourceValue(resourceUsage.ResourceKey)}");
                }
            }

            //SpecialMove
            foreach (var specialMovement in currentActionData.SpecialMovementList)
            {
                if (currentFrame >= specialMovement.StartFrame && currentFrame <= specialMovement.EndFrame)
                {
                    ApplySpecialMovement(specialMovement);
                    
                }
                else
                {
                    if (AnimatorHasLayer(animator, 1))
                    {
                        animator.SetLayerWeight(1, 0);
                    }
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

                    // ���� �÷��̾��� �̵��� ������ ����ȭ
                    if (isLocalPlayer)
                    {
                        transform.position += finalMoveVector;
                        CmdSyncMovement(finalMoveVector);
                    }
                }
            }

            foreach (var spawnData in currentActionData.ActionSpawnBulletList)
            {
                // �ߺ��� ��ȯ�� ����: HashSet�� ���Ե��� ���� ��쿡�� ��ȯ
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

                    // Bullet �ν��Ͻ� ���� �� �ʱ�ȭ
                    BulletBehaviour bullet = Instantiate(spawnData.BulletPrefab, spawnPosition, Quaternion.LookRotation(spawnDirection));
                    NetworkServer.Spawn(bullet.gameObject);
                    bullet.Initialize(this, spawnDirection);

                    // �ߺ� ������ HashSet�� �߰�
                    spawnedBulletData.Add(spawnData);
                }
            }


            foreach (var vfxData in currentActionData.ActionSpawnVfxList)
            {
                if (currentFrame >= vfxData.SpawnFrame && !spawnedVfxData.Contains(vfxData))
                {
                    SpawnVfx(vfxData);
                    spawnedVfxData.Add(vfxData); // �ߺ� ��ȯ ������ �߰�
                }
            }

            // Manual VfxObject���� �ð� ����
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

                        if (target == null || target == this)
                        {
                            continue;
                        }

                        if (IsValidTarget(target))
                        {
                            if (hitTargets.ContainsKey(target) && hitTargets[target].Contains(hitbox.HitGroup))
                            {
                                continue;
                            }

                            // ��Ʈ�� ���� �÷��̾�� ������ ��û
                            if (isServer)
                            {
                                var hit = currentActionData.HitIdList.Find(x => x.HitId == hitbox.HitId);
                                hit.Attacker = this;
                                hit.Victim = target;
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


    protected void HandleHit(HitData hit)
    {
        if (hit.Victim == null) return;

        // �������� ���� ���� ��û
        if (isServer)
        {
            hit.Victim.TakeDamage(hit);
            hit.Victim.RpcTakeDamage(hit); ;

            OnHit(hit);
            RpcOnHit(hit);

            ApplyHitStop(hit.HitStopFrame);
            RpcApplyHitStop(hit.HitStopFrame);
            target.ApplyHitStop(hit.HitStopFrame);
            target.RpcApplyHitStop(hit.HitStopFrame);
        }
        else
        {
            CmdApplyHitStop(hit.HitStopFrame);
        }

        Debug.Log($"Hit {target.name} for {hit.HitDamage} damage with {hit.HitStopFrame} frames of hitstop.");
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

        // �ִϸ��̼� ����
        if (animator != null)
        {
            animator.speed = 0f;
        }

        // Manual VFX�� ����
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

        // �ִϸ��̼� �簳
        if (animator != null)
        {
            animator.speed = 1f;
        }

        // Manual VFX �簳
        foreach (var vfx in activeManualVfxList)
        {
            float vfxTime = (currentFrame - vfx.spawnFrame) / 60f;
            vfx.SetTime(vfxTime); // �ð� �缳��
            vfx.particle.Play();
        }
    }

   

    protected virtual void EndAction()
    {
        hitTargets.Clear();
        ChangeStatePrev(CharacterState.Idle);
        //currentFrame = 0;
        foreach(var vfx in activeManualVfxList)
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

                    // ȸ�� ó��
                    if (specialMovementData.CanRotate)
                    {
                        targetRotation = Quaternion.LookRotation(inputDirection);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
                    }

                    // �ִϸ����Ϳ� ���� ��ǲ ��ȣ ����
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
                    
                    
                }
                else
                {
                    // ��ǲ�� ���� ��� �ӵ��� ����
                    currentSpeed = Mathf.Lerp(currentSpeed, 0, Time.deltaTime);
                    moveVector = HandleCollisionAndSliding(Vector3.zero, currentSpeed) * specialMovementData.Value;
                    transform.position += moveVector;

                    // �ִϸ����� �ӵ� ����
                    if (AnimatorHasParameter(animator, "X"))
                    {
                        animator.SetFloat("X", Mathf.Lerp(animator.GetFloat("X"), 0, Time.deltaTime * 10f));
                    }
                    if (AnimatorHasParameter(animator, "Z"))
                    {
                        animator.SetFloat("Z", Mathf.Lerp(animator.GetFloat("Z"), 0, Time.deltaTime * 10f));
                    }
                }

                if (AnimatorHasLayer(animator, 1))
                {
                    animator.SetLayerWeight(1, 1);
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


    public virtual void TakeDamage(HitData hit)
    {
        currentHealth -= hit.HitDamage;

        // �����ڰ� �ִٸ� �� ������ �ٶ󺸰� ��
        if (hit.Attacker != null)
        {
            Vector3 directionToAttacker = (hit.Attacker.transform.position - transform.position).normalized;
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
            if (hit.hitType == HitType.DamageOnly)
            {
                return;
            }

            switch (hit.hitType)
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

        OnAttacked(hit.Attacker);
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

        BuffManager.Instance.TriggerBuffEffect(BuffTriggerType.OwnerHit, hit);
        BuffManager.Instance.TriggerBuffEffect(BuffTriggerType.EveryHit, hit);

        StartCoroutine(ResetHitRecentlyFlag());
    }

    [Command]
    public void CmdOnHit(HitData hit)
    {
        // �������� ó�� �� Ŭ���̾�Ʈ�� ����ȭ
        OnHit(hit);
        RpcOnHit(hit);
    }

    [ClientRpc]
    public void RpcOnHit(HitData hit)
    {
        if (!isServer)
        {
            // �ٸ� Ŭ���̾�Ʈ���� ó��
            OnHit(hit);
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

    private void StartKnockBack(HitData hit)
    {
        ChangeStatePrev(CharacterState.KnockBack);
        knockBackDirection = hit.Direction;
        knockBackSpeed = hit.KnockbackPower;

        // HitStun�� ������ ������ ���
        knockBackDuration = Mathf.Max(hit.HitStunFrame / 60f, knockBackSpeed / 10f);
        knockBackTimer = 0f;

        animator.Play("Knockback", 0, 0f);
    }

    [Command]
    public void CmdStartKnockBack(HitData hit)
    {
        StartKnockBack(hit); // �������� ���� ����
        RpcStartKnockBack(hit); // Ŭ���̾�Ʈ�� ����ȭ
    }

    [ClientRpc]
    private void RpcStartKnockBack(HitData hit)
    {
        if (!isServer)
        {
            StartKnockBack(hit); // Ŭ���̾�Ʈ���� ���� ���� ����
        }
    }

    private void StartKnockBackSmash(HitData hit)
    {
        ChangeStatePrev(CharacterState.KnockBackSmash);
        currentState = CharacterState.KnockBackSmash; // ���ÿ��� ���� ����
        knockBackDirection = hit.Direction.normalized;
        knockBackSpeed = hit.KnockbackPower;

        // HitStun�� ������ ������ ���
        knockBackDuration = Mathf.Max(hit.HitStunFrame / 60f, knockBackSpeed / 10f);
        knockBackTimer = 0f;

        animator.Play("Knockback", 0, 0f);
    }

    [Command]
    public void CmdStartKnockBackSmash(HitData hit)
    {
        StartKnockBackSmash(hit); // �������� ���� ����
        RpcStartKnockBackSmash(hit); // Ŭ���̾�Ʈ�� ����ȭ
    }

    [ClientRpc]
    private void RpcStartKnockBackSmash(HitData hit)
    {
        if (!isServer)
        {
            StartKnockBackSmash(hit);
        }

    }

    private float initialBurstDuration = 0.2f; // �ʱ� ���� �˹� ����
    private float flightDuration = 0.3f;  // ������ �ӵ��� ���ư��� �ð� (���� ����)
    private float decelerationDuration = 0.3f;  // ���� �ð�
    private float totalDuration => initialBurstDuration + flightDuration + decelerationDuration; // �� �˹� ���� �ð�

    public bool IsHitStopped { get => isHitStopped; set => isHitStopped = value; }

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

            // ���� �߰��� ���� ������ ���� ������ ���⿡ �߰��մϴ�.

            default:
                Debug.LogWarning($"�� �� ���� ���� Ÿ��: {condition.Type}");
                return false;
        }
    }

    public void StartAction(ActionKey actionKey)
    {
        var actionsForKey = characterData.ActionTable
        .Where(entry => entry.ActionKey == actionKey)
        .Select(entry => entry.ActionData)
        .ToList();

        foreach (var actionData in actionsForKey)
        {
            // ������ üũ�Ͽ� ������ �� �ִ��� Ȯ��
            bool canExecute = true;
            foreach (var condition in actionData.Conditions)
            {
                if (!CheckActionConditions(condition))
                {
                    int currentResourceCount = resourceTable.GetResourceValue(condition.ResourceKey);
                    Debug.Log($"���� ���������� �׼� ���� �Ұ�: {actionKey}. �䱸 ���ҽ�: {condition.ResourceKey}, �䱸 ����: {condition.Count}, ���� ����: {currentResourceCount}");
                    canExecute = false;
                    break;
                }
            }

            // ���� ���� ���ο� ���� �׼� ���� �Ǵ� ���� �׼����� �̵�
            if (canExecute)
            {
                Debug.Log($"Starting action: {actionKey}");

                // ������ �����ϴ� ActionData�� �����ϵ��� ����
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
                }
                hitTargets.Clear();

                // ������ �����Ͽ� ù ��°�� ������ �����ϴ� �׼Ǹ� ����
                return;
            }
        }

        // ��� �׼��� �˻������� ���� ������ �׼��� ���� ���
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
    ///vfx ���� �ð� ó�� manual
    ///
    private struct ManualVfxInfo
    {
        public VfxObject Obj;
        public float StartFrame;
        public float EndFrame;
    }

    //animator �з����� �˻�
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

    // Action���� ���𰡸� ��ȯ�ϴ� �޼���
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
            activeManualVfxList.Add(vfx); // Manual Ÿ�Ը� ���� ����Ʈ�� �߰��Ͽ� Hitstop�� ���� �ް� ����
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
        // �ʱ� ��ġ ���� (ĳ���� �Ӹ� ��)
        hpStaminaBarController = hpStaminaBarInstance.GetComponentInChildren<HpStaminaBarController>();
        hpStaminaBarController.target = this;
    }
}
