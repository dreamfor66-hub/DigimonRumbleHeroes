using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class CharacterBehaviour : MonoBehaviour
{
    public CharacterData characterData;
    private Collider[] hurtboxColliders;

    protected Animator animator;
    protected Vector3 direction;
    [SerializeField]
    protected float currentSpeed;
    protected float stopTime = 0.05f;
    protected float stopTimer;

    [ShowInInspector]
    protected CharacterState currentState = CharacterState.Idle;
    protected ActionKey currentActionKey;

    protected float currentFrame;

    [ShowInInspector, ReadOnly]
    protected float currentHealth;

    protected Dictionary<CharacterBehaviour, List<int>> hitTargets;

    public SphereCollider collisionCollider;

    // Knockback 관련 변수들
    private Vector3 knockBackDirection;
    private float knockBackSpeed;
    private float knockBackDuration;
    private float knockBackTimer;

    // HitStop 관련 변수들
    private float hitStopTimer; // HitStop 시간을 관리하는 타이머
    private bool isHitStopped; // HitStop 상태를 나타내는 변수

    // target 관련 변수
    public CharacterBehaviour target;
    private GameObject targetIndicator;

    protected virtual void Start()
    {
        animator = GetComponentInChildren<Animator>();
        currentSpeed = characterData.moveSpeed;
        hitTargets = new Dictionary<CharacterBehaviour, List<int>>();
        InitializeHurtboxes();
        InitializeCollisionCollider();
        InitializeHealth();
        EntityContainer.Instance.RegisterCharacter(this);
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
        currentHealth = characterData.maxHealth;
    }

    protected virtual void Update()
    {
        if (isHitStopped)
        {
            hitStopTimer -= Time.deltaTime;
            if (hitStopTimer <= 0f)
            {
                ResumeAfterHitStop();
            }
            return;
        }

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
                HandleKnockback();
                break;
            case CharacterState.KnockBackSmash:
                HandleKnockbackSmash();
                break;
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
            animator.Play(currentActionData.AnimationKey, 0, animationFrame / GetClipTotalFrames(currentActionData.AnimationKey));

            // TransformMove
            foreach (var movement in currentActionData.MovementList)
            {
                if (currentFrame >= movement.StartFrame && currentFrame <= movement.EndFrame)
                {
                    // 프레임 사이의 비율 계산 (0부터 1까지의 값)
                    float t = (currentFrame - movement.StartFrame) / (float)(movement.EndFrame - movement.StartFrame);

                    // StartValue에서 EndValue로 선형 보간
                    Vector2 interpolatedSpeed = Vector2.Lerp(movement.StartValue, movement.EndValue, t);

                    // 보간된 값을 이동 벡터로 변환
                    Vector3 moveVector = new Vector3(interpolatedSpeed.x, 0, interpolatedSpeed.y);

                    // 충돌과 슬라이딩을 처리한 후 이동
                    Vector3 finalMoveVector = HandleCollisionAndSliding(moveVector.normalized, moveVector.magnitude);

                    // 최종적으로 캐릭터의 위치를 업데이트
                    transform.position += finalMoveVector;
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

                            HandleHit(hitbox.HitId, target, currentActionData);

                            if (!hitTargets.ContainsKey(target))
                            {
                                hitTargets[target] = new List<int>();
                            }
                            hitTargets[target].Add(hitbox.HitGroup);

                            break;
                        }
                    }
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

    protected void ApplyHitStop(float durationInFrames)
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

    protected void ResumeAfterHitStop()
    {
        isHitStopped = false;

        // 애니메이션 재개
        if (animator != null)
        {
            animator.speed = 1f;
        }
    }

    protected void HandleHit(int hitId, CharacterBehaviour target, ActionData currentActionData)
    {
        var hitData = currentActionData.HitIdList.Find(hit => hit.HitId == hitId);

        if (hitData != null)
        {
            Vector3 hitDirection = (target.transform.position - transform.position).normalized;
            target.TakeDamage(hitData.HitDamage, hitDirection, hitData);

            // HitStop을 프레임 단위로 계산
            float hitStopDuration = hitData.HitStopFrame;
            ApplyHitStop(hitStopDuration);
            target.ApplyHitStop(hitStopDuration);

            Debug.Log($"Hit {target.name} for {hitData.HitDamage} damage with {hitData.HitStopFrame} frames of hitstop.");
        }
    }

    protected virtual void EndAction()
    {
        hitTargets.Clear();
        currentState = CharacterState.Idle;
        currentFrame = 0;
    }

    protected void TakeDamage(float damage, Vector3 hitDirection, HitData hitData)
    {
        currentHealth -= damage;

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
                    animator.SetTrigger("hit");
                    StartKnockBack(hitDirection, hitData);
                    break;

                case HitType.Strong:
                    animator.SetTrigger("hit");
                    StartKnockBackSmash(hitDirection, hitData);
                    break;
            }
        }
    }

    private void StartKnockBack(Vector3 hitDirection, HitData hitData)
    {
        currentState = CharacterState.KnockBack;
        knockBackDirection = hitDirection.normalized;
        knockBackSpeed = hitData.KnockbackPower;

        // HitStun을 프레임 단위로 계산
        knockBackDuration = Mathf.Max(hitData.HitStunFrame / 60f, knockBackSpeed / 10f);
        knockBackTimer = 0f;

        animator.Play("Knockback");
    }

    private void StartKnockBackSmash(Vector3 hitDirection, HitData hitData)
    {
        currentState = CharacterState.KnockBackSmash;
        knockBackDirection = hitDirection.normalized;
        knockBackSpeed = hitData.KnockbackPower;

        // HitStun을 프레임 단위로 계산
        knockBackDuration = Mathf.Max(hitData.HitStunFrame / 60f, knockBackSpeed / 10f);
        knockBackTimer = 0f;

        animator.Play("Knockback");
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
            currentState = CharacterState.Idle;
            knockBackSpeed = 0f;
        }
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
            currentState = CharacterState.KnockBack;
            knockBackDuration = Mathf.Max(knockBackTimer, knockBackSpeed / 10f);
        }

        if (knockBackTimer >= totalDuration || knockBackSpeed < 0.1f)
        {
            currentState = CharacterState.Idle;
            knockBackSpeed = 0f;
        }
    }


    protected virtual void Die()
    {
        animator.SetTrigger("die");
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
        currentState = CharacterState.Action;
        currentFrame = 0;

        hitTargets.Clear();
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
                StartAction(ActionKey.Basic01);
                break;
            case InputMessage.B:
                StartAction(ActionKey.Special01); // 이전 Dash 대신 Special01로 대체
                break;
            case InputMessage.C:
                StartAction(ActionKey.Special02); // 추가적인 스페셜 액션
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
