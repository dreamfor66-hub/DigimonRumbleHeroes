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
    [SyncVar]
    protected Vector3 direction;
    [SyncVar]
    public float currentSpeed;
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
    [SerializeField,Sirenix.OdinInspector.ReadOnly, SyncVar]
    public float maxHealth;

    protected Dictionary<CharacterBehaviour, List<int>> hitTargets;

    public SphereCollider collisionCollider;

    // Knockback ���� ������
    [SyncVar]
    private Vector3 knockBackDirection;
    [SyncVar] [SerializeField]
    private float initialKnockBackSpeed; //���� �ӵ�
    [SyncVar] [SerializeField]
    private float currentknockBackSpeed; //���� �ӵ�
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

    //resource ���� ���� + Status ���� ����
    public CharacterResourceTable resourceTable;
    private List<CharacterStatusData> activeStatuses = new List<CharacterStatusData>();

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
        maxHealth = characterData.baseHP;
    }



    protected virtual void Update()
    {
        if (!isServer && !isLocalPlayer)
            return; // Ŭ���̾�Ʈ������ ������ ������� �ʵ��� ����

        if (OnEvolution)
            return;

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
                CmdHandleAction();
                break;
            case CharacterState.KnockBack:
                CmdHandleKnockback();
                break;
            case CharacterState.KnockBackSmash:
                CmdHandleKnockbackSmash();
                break;
        }

        UpdateStatuses();

        if (Input.GetKeyDown(KeyCode.Keypad1))
        {
            if (isLocalPlayer)
            {
                CmdHandleEvolution(0);
            }
                 // ù ��° EvolutionInfo
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

        if (isLocalPlayer)
        {
            CmdUpdateSpeed(currentSpeed);
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

    void UpdateSpeed(float value)
    {
        currentSpeed = value;
    }
    [Command]
    void CmdUpdateSpeed(float value)
    {
        UpdateSpeed(value);
        RpcUpdateSpeed(value);
    }
    [ClientRpc]
    void RpcUpdateSpeed(float value)
    {
        if (isServer) return;
        UpdateSpeed(value);
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


    protected virtual void HandleIdle()
    {
        if (AnimatorHasLayer(animator, 1))
            animator.SetLayerWeight(1, 0);
    }

    protected virtual void HandleMovement()
    {
        if (AnimatorHasLayer(animator, 1))
            animator.SetLayerWeight(1, 0);
    }

    [Command]
    void CmdHandleAction()
    {
        HandleAction();
        RpcHandleAction();
    }

    [ClientRpc]
    void RpcHandleAction()
    {
        if (!isServer)
            HandleAction();
    }

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
            if (isLocalPlayer || isServer)
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

                            // ��Ʈ�� ���� �÷��̾�� ������ ��û
                            if (isServer)
                            {
                                var hit = currentActionData.HitIdList.Find(x => x.HitId == hitbox.HitId).Clone();

                                hit.Attacker = this;
                                hit.Victim = target;
                                hit.Direction = (new Vector3(target.transform.position.x, 0, target.transform.position.z) - new Vector3(transform.position.x, 0, transform.position.z)).normalized;
                                hit.Direction.y = 0;
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
            // TransformMove
            foreach (var movement in currentActionData.MovementList)
            {
                if (currentFrame >= movement.StartFrame && currentFrame <= movement.EndFrame)
                {
                    // ���� �÷��̾��� �̵��� ������ ����ȭ
                    if (isLocalPlayer || isServer)
                    {
                        float t = (currentFrame - movement.StartFrame) / (float)(movement.EndFrame - movement.StartFrame);
                        Vector2 interpolatedSpeed = Vector2.Lerp(movement.StartValue, movement.EndValue, t);
                        Vector3 moveVector = transform.forward * interpolatedSpeed.y + transform.right * interpolatedSpeed.x;
                        Vector3 finalMoveVector = HandleCollisionAndSliding(moveVector.normalized, moveVector.magnitude);

                        CmdSyncMovement(finalMoveVector);
                    }
                }
            }

            foreach (var transition in currentActionData.Transition)
            {
                if (currentFrame >= transition.StartFrame && currentFrame <= transition.EndFrame)
                {
                    // ť���� �Է� �޽��� Ȯ��
                    switch (transition.Type)
                    {
                        case TransitionType.Always:
                            {
                                EndAction();
                                if (isServer)
                                {
                                    StartAction(transition.NextAction);
                                    RpcStartAction(transition.NextAction);
                                }
                                else if (isLocalPlayer || isServer)
                                {
                                    CmdStartAction(transition.NextAction);
                                }
                                return; // ���ο� �׼� ���� �� ����
                                break;
                            }
                        case TransitionType.Input:
                            {
                                while (inputQueue.Count > 0)
                                {
                                    var input = inputQueue.Dequeue();

                                    if (input == transition.InputType)
                                    {
                                        // Transition ���ǿ� ������ ���� �׼� ���� �� ���ο� �׼� ����
                                        EndAction();
                                        if (isServer)
                                        {
                                            StartAction(transition.NextAction);
                                            RpcStartAction(transition.NextAction);
                                        }
                                        else if (isLocalPlayer)
                                        {
                                            CmdStartAction(transition.NextAction);
                                        }
                                        return; // ���ο� �׼� ���� �� ����
                                    }
                                }
                                break;
                            }
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
                    // BulletData�κ��� SerializedBulletData ����
                    var bulletData = spawnData.BulletPrefab.bulletData;
                    SerializedBulletData serializedData = new SerializedBulletData(
                        bulletData.LiftTime,
                        bulletData.Speed,
                        bulletData.HitboxList,
                        bulletData.HitIdList
                    );

                    // ����/Ŭ���̾�Ʈ �����Ͽ� ��ȯ ���� ȣ��
                    //if (isServer)
                    //{
                    //    //ActionSpawnBullet(spawnPosition, spawnDirection, serializedData, spawnData.BulletPrefab.name);
                    //    //RpcActionSpawnBullet(spawnPosition, spawnDirection, serializedData, spawnData.BulletPrefab.name);
                    //}
                    //else
                        CmdActionSpawnBullet(spawnPosition, spawnDirection, serializedData, spawnData.BulletPrefab.name);


                    spawnedBulletData.Add(spawnData); // �ߺ� ����
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
        if (!isServer)
        {
            animator.Play(animationKey, 0, normalizedTime);
        }
    }

    // ������ ĳ���� �̵� ����ȭ ��û
    [Command]
    private void CmdSyncMovement(Vector3 moveVector)
    {
        transform.position += moveVector;
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

        // �������� ���� ���� ��û
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
        if (AnimatorHasLayer(animator, 1))
            animator.SetLayerWeight(1, 0);
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
                    if (AnimatorHasLayer(animator, 1))
                    {
                        // ���� ����ġ�� Animator���� ������
                        float currentLayerWeight = animator.GetLayerWeight(1);

                        // ��ǥ ����ġ ����
                        float targetLayerWeight = 1f; // Ȱ��ȭ�Ϸ��� 1f�� ����

                        // ����ġ�� �ε巴�� ��ȭ��Ŵ
                        float newLayerWeight = Mathf.Lerp(currentLayerWeight, targetLayerWeight, Time.deltaTime * 5f);

                        // Animator�� ���ο� ����ġ ����
                        animator.SetLayerWeight(1, newLayerWeight);
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
                    if (AnimatorHasLayer(animator, 1))
                    {
                        // ���� ����ġ�� Animator���� ������
                        float currentLayerWeight = animator.GetLayerWeight(1);

                        // ��ǥ ����ġ ����
                        float targetLayerWeight = 0f; // Ȱ��ȭ�Ϸ��� 1f�� ����

                        // ����ġ�� �ε巴�� ��ȭ��Ŵ
                        float newLayerWeight = Mathf.Lerp(currentLayerWeight, targetLayerWeight, Time.deltaTime * 5f);

                        // Animator�� ���ο� ����ġ ����
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

    private void ActionSpawnBullet(Vector3 position, Vector3 direction, SerializedBulletData serializedData, string prefabName)
    {
        GameObject bulletPrefab = NetworkManager.singleton.spawnPrefabs.Find(prefab => prefab.name == prefabName);
        if (bulletPrefab == null)
        {
            Debug.LogError($"Prefab '{prefabName}' not found in RegisteredSpawnablePrefabs.");
            return;
        }

        // �������� Bullet ����
        GameObject bulletObject = Instantiate(bulletPrefab, position, Quaternion.LookRotation(direction));

        NetworkServer.Spawn(bulletObject);
        // Bullet �ʱ�ȭ
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
        if (isServer) return; // ���������� �̹� ó���Ǿ����Ƿ� Ŭ���̾�Ʈ�� ����
        ActionSpawnBullet(position, direction, serializedData, prefabName);
    }

    public virtual void TakeDamage(HitData hit)
    {
        currentHealth -= Mathf.Clamp(hit.HitDamage,0, hit.HitDamage);

        // �����ڰ� �ִٸ� �� ������ �ٶ󺸰� ��
        if (hit.Attacker != null)
        {
            Vector3 directionToAttacker = (new Vector3 (hit.Attacker.transform.position.x , 0 , hit.Attacker.transform.position.z) - new Vector3(transform.position.x, 0, transform.position.z)).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(directionToAttacker);
            transform.rotation = lookRotation; // ��� ȸ��
        }
        OnAttacked(hit.Attacker);

        // ������ �ؽ�Ʈ ǥ��
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
            if (hit.HitType == HitType.DamageOnly || HasStatus(StatusType.SuperArmor))
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
        initialKnockBackSpeed = hit.KnockbackPower;

        // HitStun�� ������ ������ ���
        knockBackDuration = Mathf.Max(hit.HitStunFrame / 60f, initialKnockBackSpeed / 10f);
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
        //currentState = CharacterState.KnockBackSmash; // ���ÿ��� ���� ����
        knockBackDirection = hit.Direction.normalized;
        initialKnockBackSpeed = hit.KnockbackPower;

        // HitStun�� ������ ������ ���
        knockBackDuration = Mathf.Max(hit.HitStunFrame / 60f, initialKnockBackSpeed / 10f);
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


        //���浹
        RaycastHit hit;
        var radius = characterData.colliderRadius;
        LayerMask wallLayer = LayerMask.GetMask("WallCollider");
        float knockbackPowerForWallCollide = Mathf.Max(initialKnockBackSpeed * ResourceHolder.Instance.gameVariables.collideReduceMultiplier, 5f);
        if (Physics.SphereCast(transform.position, radius, knockBackDirection, out hit, knockBackMovement.magnitude, wallLayer))
        {
            HitData wallHitData = new HitData
            {
                Attacker = null, // ���� attacker�� �ƴ�
                Victim = this, // 
                Direction = Vector3.Reflect(knockBackDirection, hit.normal).normalized,
                KnockbackPower = currentknockBackSpeed * ResourceHolder.Instance.gameVariables.collideReduceMultiplier, // �ӵ� ����
                HitType = HitType.Strong, // �ӵ� ����
                HitDamage = 0f, // �� �浹�� ����� ����
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


        // �ٸ� CharacterBehaviour���� �浹 ó��
        Collider[] colliders = Physics.OverlapSphere(transform.position, radius);
        foreach (var collider in colliders)
        {
            CharacterBehaviour otherCharacter = collider.GetComponentInParent<CharacterBehaviour>();

            // �ڽ��� �ƴϰ�, ��밡 KnockBackSmash ���°� �ƴ� ��츸 ó��
            if (otherCharacter != null && otherCharacter != this && (otherCharacter.teamType is not TeamType.Player && teamType is not TeamType.Player) &&
                (otherCharacter.currentState != CharacterState.KnockBackSmash || otherCharacter.currentState != CharacterState.KnockBack) &&
                knockBackTimer > ResourceHolder.Instance.gameVariables.collideAvoidTime)
            {
                // ���� ������� ���� Ȯ��
                Vector3 toOtherCharacter = (otherCharacter.transform.position - transform.position).normalized;
                float angleToOther = Vector3.Angle(knockBackDirection, toOtherCharacter);

                if (angleToOther > ResourceHolder.Instance.gameVariables.maxCollisionAngle) // ������ ������ �ʰ��ϸ� ����
                    continue;

                Vector3 collisionNormal = toOtherCharacter;

                // ��ġ ����
                float overlapDistance = radius * 2f - Vector3.Distance(transform.position, otherCharacter.transform.position);
                transform.position -= collisionNormal * (overlapDistance / 2f);
                otherCharacter.transform.position += collisionNormal * (overlapDistance / 2f);

                // �浹�� ���� ���ο� HitData ����
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

                // �˹� �� HitStop ó��
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

            // ���� �߰��� ���� ������ ���� ������ ���⿡ �߰��մϴ�.

            default:
                Debug.LogWarning($"�� �� ���� ���� Ÿ��: {condition.Type}");
                return false;
        }
    }

    public virtual void StartAction(ActionKey actionKey)
    {
        var actionsForKey = characterData.ActionTable
        .Where(entry => entry.ActionKey == actionKey)
        .Select(entry => entry.ActionData)
        .ToList();

        foreach (var originActionData in actionsForKey)
        {

            var actionData = originActionData.Clone();
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
                    animator.SetFloat("X", 0);
                    animator.SetFloat("Z", 0);
                }
                hitTargets.Clear();

                BuffManager.Instance.TriggerBuffEffect(BuffTriggerType.OwnerAction, actionData);

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
        var radius = characterData.colliderRadius;
        LayerMask wallLayer = LayerMask.GetMask("WallCollider");
        LayerMask characterLayer = LayerMask.GetMask("Character");

        Vector3 finalDirection = moveDirection;
        float finalSpeedAdjustment = 1f;
        float moveDistance = moveSpeed * Time.deltaTime;
        const float collisionEpsilon = 0.01f; // ������ ���� �Ÿ�
        const float minMovementThreshold = 0.001f; // �ּ� �̵� �Ӱ谪 (���� ����)

        // 1. �� �浹 ó��
        List<RaycastHit> hits = Physics.SphereCastAll(transform.position, radius, finalDirection, moveDistance, wallLayer).ToList();
        if (hits.Count > 0)
        {
            // �浹�� ��� ���� ������ ���
            Vector3 averageNormal = Vector3.zero;
            float minDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                averageNormal += hit.normal;
                if (hit.distance < minDistance)
                {
                    minDistance = hit.distance;
                }
            }

            // ��� ���� ���
            averageNormal.Normalize();

            // ħ���� ��� �о�� ó��
            if (minDistance <= collisionEpsilon)
            {
                float pushOutDistance = radius - minDistance + collisionEpsilon;
                Vector3 pushOutDirection = averageNormal * pushOutDistance;

                // �ʹ� ������ �о�� ����
                pushOutDirection = Vector3.ClampMagnitude(pushOutDirection, 0.1f);

                transform.position += pushOutDirection; // ������ ��¦ �о
                moveDistance -= pushOutDistance;       // �̵� �Ÿ� ����
            }

            // �̵� ������ ���� ���� �������� �̲����߸�
            Vector3 slideDirection = Vector3.ProjectOnPlane(finalDirection, averageNormal).normalized;

            float angle = Vector3.Angle(finalDirection, averageNormal);
            float speedAdjustment = Mathf.Clamp01(1 - Mathf.Abs(angle - 90) / 90f);

            // �̵����� �ʹ� ������ ���� ó��
            if (moveDistance < minMovementThreshold)
            {
                return Vector3.zero; // ���� ������ ���� ����
            }

            finalDirection = slideDirection;
            finalSpeedAdjustment *= speedAdjustment;
        }
        // ĳ���� �浹 ó�� (SphereCastAll ���)
        List<RaycastHit> characterHits = Physics.SphereCastAll(transform.position, radius, finalDirection, moveDistance, characterLayer).ToList();

        // �ڱ� �ڽ� ����
        characterHits.RemoveAll(hit => hit.collider.gameObject == gameObject);

        foreach (var hit in characterHits)
        {
            CharacterBehaviour otherCharacter = hit.collider.GetComponentInParent<CharacterBehaviour>();

            if (otherCharacter != null && otherCharacter != this)
            {
                float myMass = characterData.mass;
                float otherMass = otherCharacter.characterData.mass;

                Vector3 toOtherCharacter = (otherCharacter.transform.position - transform.position).normalized;

                float angleToOther = Vector3.Angle(finalDirection, toOtherCharacter);
                if (angleToOther > 90f)
                    continue;

                Vector3 characterNormal = toOtherCharacter;

                // �浹 ���� ���
                float totalMass = myMass + otherMass;
                float myPushFactor = otherMass / totalMass;
                float otherPushFactor = myMass / totalMass;

                // ���� �о��
                Vector3 pushDirection = characterNormal * moveDistance * otherPushFactor;
                if (!TryPushCharacter(otherCharacter, pushDirection, radius, wallLayer, characterLayer))
                {
                    // �о �� ���� ��� �̵� �ߴ�
                    Vector3 slideDirection = Vector3.ProjectOnPlane(finalDirection, characterNormal).normalized;


                    // �̲����� ����� �̵�
                    finalDirection = slideDirection;
                    finalSpeedAdjustment *= myPushFactor; // �ӵ� ����
                    continue;
                }

                // �� �̵� ó��
                Vector3 successfulSlideDirection = Vector3.ProjectOnPlane(finalDirection, characterNormal).normalized;
                finalDirection = successfulSlideDirection;
                finalSpeedAdjustment *= myPushFactor;
            }
        }

        // ���� �̵� ���� �� �ӵ� ���
        
        return finalDirection * moveSpeed * finalSpeedAdjustment * Time.deltaTime;
    }


    private bool TryPushCharacter(CharacterBehaviour target, Vector3 pushDirection, float radius, LayerMask wallLayer, LayerMask characterLayer)
    {
        Vector3 targetNewPosition = target.transform.position + pushDirection;

        // �� �浹 �˻�
        if (Physics.CheckSphere(targetNewPosition, radius, wallLayer))
        {
            return false; // �о �� ����
        }

        // �ٸ� ĳ���Ϳ� �浹 �˻�
        Collider[] characterOverlaps = Physics.OverlapSphere(targetNewPosition, radius, characterLayer);
        foreach (var overlap in characterOverlaps)
        {
            CharacterBehaviour otherCharacter = overlap.GetComponentInParent<CharacterBehaviour>();
            if (otherCharacter != null && otherCharacter != target)
            {
                Vector3 toOtherCharacter = (otherCharacter.transform.position - targetNewPosition).normalized;
                if (Vector3.Angle(pushDirection, toOtherCharacter) < 90f)
                {
                    // ��������� �о�� �õ�
                    if (!TryPushCharacter(otherCharacter, pushDirection, radius, wallLayer, characterLayer))
                    {
                        return false;
                    }
                }
            }
        }

        // �о�� ����
        target.transform.position = targetNewPosition;
        return true;
    }

    private void UpdateStatuses()
    {
        // ����� ���� ����
        activeStatuses.RemoveAll(status => status.IsExpired);
    }

    /// <summary>
    /// �������� ���¸� �߰��ϰ�, Ŭ���̾�Ʈ�� ����ȭ
    /// </summary>
    [Command]
    public void CmdAddStatus(StatusType statusType, float duration)
    {
        AddStatus(statusType, duration); // �������� ���� �߰�
        RpcAddStatus(statusType, duration); // Ŭ���̾�Ʈ�� ����ȭ
    }
    [ClientRpc]
    private void RpcAddStatus(StatusType statusType, float duration)
    {
        if (isServer) return; // ������ �̹� ����Ǿ����Ƿ� ����
        AddStatus(statusType, duration);
    }
    public void AddStatus(StatusType statusType, float duration)
    {
        var existingStatus = activeStatuses.Find(status => status.StatusType == statusType);

        if (existingStatus != null)
        {
            if (existingStatus.IsPermanent)
            {
                // �̹� ���� ���¶�� ����
                return;
            }

            // ���� ���°� �ƴϸ� ���� �ð� ����
            existingStatus.ExtendDuration(duration);
        }
        else
        {
            // ���� ���� �߰�
            activeStatuses.Add(new CharacterStatusData(statusType, duration));
        }

        Debug.Log($"Added status: {statusType}, Duration: {(duration < 0 ? "Permanent" : $"{duration}s")}");
    }

    public bool HasStatus(StatusType statusType)
    {
        return activeStatuses.Exists(status => status.StatusType == statusType);
    }

    ///
    ///
    ///
    public bool OnEvolution;

    [Command]
    private void CmdHandleEvolution(int index)
    {
        HandleEvolution(index); // Host���� ����
        RpcHandleEvolution(connectionToClient.connectionId, index); // Ŭ���̾�Ʈ�� ����ȭ
    }

    [ClientRpc]
    private void RpcHandleEvolution(int connectionId, int index)
    {
        if (connectionToClient == null || connectionToClient.connectionId != connectionId)
        {
            Debug.LogWarning("�� RPC�� �ٸ� Ŭ���̾�Ʈ�� ������� �ϹǷ� �����մϴ�.");
            return;
        }

        if (characterData == null || characterData.EvolutionInfos == null || characterData.EvolutionInfos.Count <= index)
        {
            Debug.LogError($"Ŭ���̾�Ʈ���� Evolution �����Ͱ� ��ȿ���� �ʽ��ϴ�. Index: {index}");
            return;
        }

        HandleEvolution(index);
    }

    private void HandleEvolution(int index)
    {
        if (characterData == null || characterData.EvolutionInfos.Count <= index)
        {
            Debug.LogWarning($"��ȭ �����Ͱ� ��ȿ���� ����. Index: {index}");
            return;
        }

        var evolutionInfo = characterData.EvolutionInfos[index];
        if (evolutionInfo?.NextCharacter == null)
        {
            Debug.LogError($"EvolutionInfo �Ǵ� NextCharacter�� null�Դϴ�. Index: {index}");
            return;
        }

        if (OnEvolution) return; // �ߺ� ó�� ����

        if (AnimatorHasLayer(animator, 1))
        {
            animator.SetLayerWeight(1, 0);
            animator.SetFloat("X", 0);
            animator.SetFloat("Z", 0);
        }
        // 3. ��ȭ �� �ִϸ��̼� ��� �� ������
        if (isServer)
        {
            animator.Play("EvolutionStart");
            RpcPlayEvolutionStart();
        }
        else
        {
            CmdPlayEvolutionStart();
        }
        OnEvolution = true;

        StartCoroutine(HandleEvolutionCoroutine(index, evolutionInfo));
    }

    [ClientRpc]
    private void RpcPlayEvolutionStart()
    {
        if (!isServer)
        animator.Play("EvolutionStart");
    }
    
    [Command]
    private void CmdPlayEvolutionStart()
    {
        animator.Play("EvolutionStart");
        RpcPlayEvolutionStart();
    }

    private IEnumerator HandleEvolutionCoroutine(int index, EvolutionInfo evolutionInfo)
    {
        // ��ȭ �� �ִϸ��̼� ��� (1.5��)
        yield return new WaitForSeconds(1.5f);

        // 4. ���� ĳ������ ���� ����
        Vector3 currentPosition = transform.position;
        Quaternion currentRotation = transform.rotation;

        // 5. ���� ĳ������ ��Ʈ��ũ ���� ���� ����
        var oldConnection = connectionToClient;

        // �� ĳ���� ����
        var nextCharacterPrefab = evolutionInfo.NextCharacter.gameObject;
        var newCharacterInstance = Instantiate(nextCharacterPrefab, currentPosition, currentRotation);
        var newCharacterBehaviour = newCharacterInstance.GetComponent<CharacterBehaviour>();

        if (newCharacterBehaviour == null)
        {
            Debug.LogError("�� ĳ���� �����տ� CharacterBehaviour�� �����ϴ�.");
            OnEvolution = false; // ���� ����
            yield break;
        }

        // ��Ʈ��ũ ����ȭ
        EntityContainer.Instance.UnregisterCharacter(this);
        EntityContainer.Instance.RegisterCharacter(newCharacterBehaviour);

        // 8. ��Ʈ��ũ �󿡼� �� ��ü�� ���� ����
        // ��Ʈ��ũ ��ü ����
        if (isServer)
        {
            NetworkServer.Spawn(newCharacterInstance);
            NetworkServer.ReplacePlayerForConnection(oldConnection, newCharacterInstance);
        }
        // HpStaminaBar ���� ����ȭ
        RpcHandleHpStaminaBarDespawn();

        // ���ο� ĳ���� �ʱ�ȭ
        newCharacterBehaviour.SetLocalPlayer();
        newCharacterBehaviour.StartCoroutine(newCharacterBehaviour.HandleEvolutionComplete());

        // ���� ��ü ����
        StartCoroutine(DelayedDestroy());


        Debug.Log($"���� �÷��̾ {newCharacterBehaviour.name}(��)�� ��ȭ�߽��ϴ�.");
    }

    private IEnumerator DelayedDestroy()
    {
        yield return new WaitForEndOfFrame();
        if (isServer)
        {
            NetworkServer.Destroy(gameObject);
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

        // ��ȭ �Ϸ� ��� (0.5��)
        yield return new WaitForSeconds(1f);

        // ��ȭ �Ϸ�
        OnEvolution = false;
        Debug.Log($"��ȭ �Ϸ�: {name}");
    }

    private void PlayEvolutionCompleteAnimation()
    {
        if (animator != null)
        {
            animator.Play("EvolutionComplete");
            Debug.Log("��ȭ �Ϸ� �ִϸ��̼� ���");
        }
        else
        {
            Debug.LogWarning("Animator�� �������� �ʾ� ��ȭ �Ϸ� �ִϸ��̼��� ����� �� �����ϴ�.");
        }
    }
    [Client]
    private void SetLocalPlayer()
    {
        Debug.Log($"���� �÷��̾ {name}�� ��ȯ�Ǿ����ϴ�.");
        // ���ο� ĳ���Ϳ��� ���� �÷��̾� ó��
        if (isLocalPlayer)
        {
            Camera.main.GetComponent<CameraController>().SetTarget(transform);
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
    /// 
    private Queue<InputMessage> inputQueue = new Queue<InputMessage>();
    private IEnumerator RemoveInputAfterDelay(InputMessage message, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Queue�� ù ��° �޽����� ���� �ڷ�ƾ�� �޽����� ��ġ�� ���� ����
        if (inputQueue.Count > 0 && inputQueue.Peek() == message)
        {
            inputQueue.Dequeue();
        }
    }

    public void ReceiveInputMessage(InputMessage message)
    {
        // �Է� �޽����� ť�� ����
        inputQueue.Enqueue(message);
        StartCoroutine(RemoveInputAfterDelay(message, 0.2f));
        HandleInputMessage(message);
    }

    public virtual void HandleInputMessage(InputMessage message)
    {

        switch (currentState)
        {
            case CharacterState.Idle:
            case CharacterState.Move:
                ProcessInputMessage(message);
                inputQueue.Dequeue();
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


[System.Serializable]
public class TimedInput
{
    public InputMessage Message; // �Է� �޽���
    public float ExpiryTime;     // ����� �ð�
}