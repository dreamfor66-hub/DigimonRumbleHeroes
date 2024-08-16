using Sirenix.OdinInspector;
using System.Collections.Generic;
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

    protected virtual void Start()
    {
        animator = GetComponentInChildren<Animator>();
        currentSpeed = characterData.moveSpeed;
        hitTargets = new Dictionary<CharacterBehaviour, List<int>>();
        InitializeHurtboxes();
        InitializeCollisionCollider();
        InitializeHealth();
        EntityContainer.Instance.RegisterCharacter(this);
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
        }
    }

    protected virtual void HandleIdle() { }

    protected virtual void HandleMovement() { }

    protected void HandleAction()
    {
        if (characterData.TryGetActionData(currentActionKey, out ActionData currentActionData))
        {
            currentFrame += Time.deltaTime * 60f;

            float animationFrame = currentActionData.AnimationCurve.Evaluate(currentFrame);
            animator.Play(currentActionData.AnimationKey, 0, animationFrame / GetClipTotalFrames(currentActionData.AnimationKey));

            //TransformMove
            foreach (var movement in currentActionData.MovementList)
            {
                if (currentFrame >= movement.StartFrame && currentFrame <= movement.EndFrame)
                {
                    // ������ ������ ���� ���
                    float t = (currentFrame - movement.StartFrame) / (movement.EndFrame - movement.StartFrame);

                    // �ӵ� ���
                    Vector2 currentSpeed = Vector2.Lerp(movement.StartValue, movement.EndValue, t);

                    // �̵� ó��
                    Vector3 moveVector = new Vector3(currentSpeed.x, 0, currentSpeed.y);
                    Vector3 moveDirection = transform.TransformDirection(moveVector).normalized;

                    RaycastHit hit;
                    var radius = characterData.colliderRadius;
                    LayerMask wallLayer = LayerMask.GetMask("WallCollider");

                    // ù ��° �浹�� �����ϴ� SphereCast
                    if (Physics.SphereCast(transform.position, radius, moveDirection, out hit, moveVector.magnitude * Time.deltaTime, wallLayer))
                    {
                        // ù ��° ���� ǥ�� ����� ��´�
                        Vector3 normal = hit.normal;

                        // ù ��° ���� ���� �̲����� ������ ���
                        Vector3 slideDirection = Vector3.ProjectOnPlane(moveDirection, normal).normalized;

                        // ù ��° ���� ���� ���� ó��
                        float firstAngle = Vector3.Angle(moveDirection, normal);
                        float firstSpeedAdjustment = Mathf.Clamp01(1 - Mathf.Abs(firstAngle - 90) / 90f);

                        // ������ �������� �� ��° �浹�� �˻��ϴ� SphereCast
                        if (Physics.SphereCast(transform.position, radius, slideDirection, out hit, moveVector.magnitude * Time.deltaTime, wallLayer))
                        {
                            // �� ��° ���� ǥ�� ����� ��´�
                            Vector3 secondNormal = hit.normal;

                            // �� ��° ���� ���� �̲����� ������ �ٽ� ���
                            slideDirection = Vector3.ProjectOnPlane(slideDirection, secondNormal).normalized;

                            // �� ��° ���� ���� ���� ó��
                            float secondAngle = Vector3.Angle(slideDirection, secondNormal);
                            float secondSpeedAdjustment = Mathf.Clamp01(1 - Mathf.Abs(secondAngle - 90) / 90f);

                            // �� ��° ���� ���� ���� ����
                            firstSpeedAdjustment *= secondSpeedAdjustment;
                        }

                        // ���� ���� �̲������鼭 �̵�, ���� ����
                        transform.position += slideDirection * currentSpeed.magnitude * firstSpeedAdjustment * Time.deltaTime;
                    }
                    else
                    {
                        // ���� ���� �ʾ��� ���� �⺻ �̵�
                        transform.position += moveDirection * currentSpeed.magnitude * Time.deltaTime;
                    }
                }
            }


            //Hitbox Cast
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

    protected void HandleHit(int hitId, CharacterBehaviour target, ActionData currentActionData)
    {
        var hitData = currentActionData.HitIdList.Find(hit => hit.HitId == hitId);

        if (hitData != null)
        {
            target.TakeDamage(hitData.HitDamage);
            Debug.Log($"Hit {target.name} for {hitData.HitDamage} damage with {hitData.HitstopTime} seconds hitstop.");
        }
    }

    protected virtual void EndAction()
    {
        hitTargets.Clear();
        currentState = CharacterState.Idle;
        currentFrame = 0;
    }

    protected void TakeDamage(float damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        else if (currentHealth > characterData.maxHealth)
        {
            currentHealth = characterData.maxHealth;
        }

        animator.SetTrigger("hit");
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

    protected virtual void MoveCharacter()
    {

        Vector3 targetPosition = transform.position + direction * currentSpeed * Time.deltaTime;
        Vector3 remainingMovement = direction * currentSpeed * Time.deltaTime;

        int maxIterations = 10; // ���� ���� ������ ���� �ִ� �ݺ� Ƚ�� ����
        int iteration = 0;

        while (remainingMovement.magnitude > 0.01f && iteration < maxIterations)
        {
            iteration++;
            if (IsCollisionDetected(targetPosition, collisionCollider.radius, out Vector3 collisionNormal, out RaycastHit[] hits))
            {
                // �浹�� �߻��� ��� �ӵ� ���� ���
                float angle = Vector3.Angle(collisionNormal, direction);
                float speedModifier = Mathf.Clamp01(1 - (angle / 90f)); // ������ ���� �ӵ� ����

                // �����̵� ���� ���
                Vector3 slideDirection = Vector3.zero;
                foreach (RaycastHit hit in hits)
                {
                    slideDirection += Vector3.ProjectOnPlane(remainingMovement, hit.normal);
                }
                slideDirection.Normalize();

                // ���� �̵��� ���
                remainingMovement = slideDirection * remainingMovement.magnitude * speedModifier;
                targetPosition = transform.position + remainingMovement;

                // �����̵� �Ŀ��� �浹�� �߻����� �ʴ��� Ȯ��
                if (!IsCollisionDetected(targetPosition, collisionCollider.radius, out _, out _))
                {
                    transform.position = targetPosition;
                    break; // �����̵� ����, ���� Ż��
                }
            }
            else
            {
                // �� �̻� �浹�� ���� ��� ���� �̵�����ŭ �̵�
                transform.position += remainingMovement;
                break;
            }
        }

        // ĳ������ ȸ��
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
    }

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
                StartAction(ActionKey.Dash);
                break;
            case InputMessage.C:
                StartAction(ActionKey.Special01);
                break;
            default:
                Debug.LogWarning("ó������ ���� InputMessage: " + message);
                break;
        }
    }

    /// <summary>
    /// ���������
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
    }
}
