using System.Collections;
using System.Linq;
using UnityEngine;
using Mirror;

public class PlayerController : CharacterBehaviour
{
    public int playerNumber;
    private Vector3 targetPosition;
    private Vector3 lastPosition;
    private bool isTouching = false;
    private bool isTapConfirmed = false;

    [SerializeField]
    [SyncVar]
    //�پ�� ���� ���� float. 1�̸� 1�ʿ� 1�� �߻�, 0.5�� 0.5�ʿ� 1�� �߻� �̷� ��
    float basicAttackCycle;

    [SyncVar]
    float basicTimer = 0f;

    [SerializeField]
    [SyncVar]
    //Update�� ���ٰ�, ���� idle�̰ų� Move�� ���ÿ� basicReady��� basic01 ���� ���� ��Ű�� ���� �����ϱ�
    bool basicReady;


    protected override void Start()
    {
        base.Start();

        teamType = TeamType.Player;
        lastPosition = transform.position;
        basicAttackCycle = characterData.defaultBasicAttackCycle;
        EntityContainer.Instance.RegisterPlayer(this);

        if (isLocalPlayer)
        {
            InitializeLocalPlayer();
        }
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void InitializeLocalPlayer()
    {
        // ���� �÷��̾� ���� ī�޶� ����
        CameraController cameraController = FindObjectOfType<CameraController>();
        if (cameraController != null)
        {
            cameraController.UpdateTarget(transform);
        }
    }

    protected override void Update()
    {
        base.Update();
        if (NetworkClient.localPlayer == null || !isLocalPlayer) return;

        HandleTargeting();

        //if (currentState is CharacterState.Idle or CharacterState.Move)
        {
            HandleInput();
        }

        // �⺻ ���� �ֱ� ó��
        basicTimer += Time.deltaTime;
        if (basicTimer >= basicAttackCycle)
        {
            basicReady = true;
        }

        // Idle �Ǵ� Move ������ ��, ���ǿ� ������ �⺻ ���� ����
        if (basicReady && (currentState == CharacterState.Idle || currentState == CharacterState.Move))
        {
            if (target != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
                if (distanceToTarget <= characterData.attackRange)
                {
                    ReceiveInputMessage(InputMessage.A);
                    basicTimer = 0f; // Ÿ�̸� �ʱ�ȭ
                    basicReady = false; // �ٽ� ��� ���·� ����
                }
            }
        }

        //// ���� �����ӿ��� ���� �̵��� �ӵ� ���
        float movementSpeed = (transform.position - lastPosition).magnitude / Time.deltaTime;

        //// ĳ���Ͱ� �̵� ���� ���� currentSpeed ������Ʈ
        //if (currentState == CharacterState.Move)
        //{
        //    currentSpeed = Mathf.Clamp(movementSpeed, , characterData.moveSpeed); // �ּ����� �ӵ��� ����
        //}
        //else
        //{
        //    currentSpeed = 0; // �̵����� ������ �ӵ��� 0���� ����
        //}

        // ���� ��ġ�� ���� �������� ���� ����
        lastPosition = transform.position;
    }

    private void HandleInput()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseInput();
#endif

#if UNITY_IOS || UNITY_ANDROID
        HandleTouchInput();
#endif
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            StartTouch(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0))
        {
            UpdateTouch(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            EndTouch(Input.mousePosition);
        }
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                StartTouch(touch.position);
            }
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                UpdateTouch(touch.position);
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                EndTouch(touch.position);
            }
        }
    }

    private void StartTouch(Vector3 position)
    {
        isTouching = true;
        initialTouchPosition = position;
        touchStartTime = Time.time;
        isTapConfirmed = false;
    }

    private void UpdateTouch(Vector3 position)
    {
        if (!isTouching)
            return;

        touchDelta = position - initialTouchPosition;
        touchDeltaDistance = touchDelta.magnitude;
        touchElapsedTime = Time.time - touchStartTime;

        if (touchElapsedTime > ResourceHolder.Instance.gameVariables.tapThreshold || touchDeltaDistance >= ResourceHolder.Instance.gameVariables.dragThreshold)
        {
            isTapConfirmed = false;

            speedMultiplier = Mathf.Clamp(touchDeltaDistance / ResourceHolder.Instance.gameVariables.maxDistance, 0f, 1f);

            inputDirection = new Vector3(touchDelta.x, 0, touchDelta.y).normalized;

            Vector3 cameraForward = Camera.main.transform.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();

            Vector3 cameraRight = Camera.main.transform.right;
            cameraRight.y = 0;
            cameraRight.Normalize();

            direction = inputDirection.x * cameraRight + inputDirection.z * cameraForward;
            currentSpeed = characterData.moveSpeed * speedMultiplier;

            if (currentState == CharacterState.Idle)
                ChangeStatePrev(CharacterState.Move);
        }
        else
        {
            if (currentState == CharacterState.Move)
                ChangeStatePrev(CharacterState.Idle);
            currentSpeed = 0;
        }
    }

    private void EndTouch(Vector3 position)
    {
        if (!isTouching)
            return;

        touchDelta = position - initialTouchPosition;
        touchDeltaDistance = touchDelta.magnitude;
        touchElapsedTime = Time.time - touchStartTime;

        if (touchDeltaDistance < ResourceHolder.Instance.gameVariables.dragThreshold && touchElapsedTime < ResourceHolder.Instance.gameVariables.tapThreshold)
        {
            ReceiveInputMessage(InputMessage.A);
        }
        else
        {
            if (currentState == CharacterState.Move)
                ChangeStatePrev(CharacterState.Idle);
        }

        isTouching = false;
        stopTimer = stopTime;
    }


    protected override void HandleIdle()
    {
        base.HandleIdle();
        if (stopTimer > 0)
        {
            currentSpeed = Mathf.Lerp(currentSpeed, 0, (stopTime - stopTimer) / stopTime);
            stopTimer -= Time.deltaTime;

            if (stopTimer <= 0)
            {
                animator.SetBool("isMoving", false);
                currentSpeed = 0;
            }
        }

        // ���� ���� ����
        animator.Play("Idle");
        animator.SetFloat("speed", 0f, 0.15f, Time.deltaTime);

        // ��Ʈ��ũ ����ȭ �κ� �߰� (���� �÷��̾��� ���� ������ ���)
        if (isLocalPlayer)
        {
            CmdSetAnimatorParameters("Idle", 0f);
        }
    }

    protected override void HandleMovement()
    {
        base.HandleMovement();
        moveVector = HandleCollisionAndSliding(direction.normalized, currentSpeed);
        transform.position += moveVector;

        transform.position = new Vector3(transform.position.x, 0, transform.position.z);

        targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);

        float normalizedSpeed = currentSpeed / characterData.moveSpeed;

        // ���� ���� ����
        animator.Play("Idle");
        animator.SetFloat("speed", normalizedSpeed);

        // ��Ʈ��ũ ����ȭ �κ� �߰� (���� �÷��̾��� ���� ������ ���)
        if (isLocalPlayer)
        {
            CmdSetAnimatorParameters("Idle", normalizedSpeed);
        }
    }

    public override void StartAction(ActionKey actionKey)
    {
        base.StartAction(actionKey);
        if (currentActionKey == ActionKey.Basic01)
            basicTimer = 0f; // Ÿ�̸� �ʱ�ȭ
            basicReady = false;
    }


    public Vector3 GetInputDirection()
    {
        return inputDirection;
    }

    public Vector3 GetInitialPosition()
    {
        return initialTouchPosition;
    }

    public float GetSpeedMultiplier()
    {
        float distance = (initialTouchPosition - Camera.main.WorldToScreenPoint(transform.position)).magnitude;
        return Mathf.Clamp(distance / ResourceHolder.Instance.gameVariables.maxDistance, 0f, 1f);
    }



    // ������ �ִϸ����� �Ű������� �����ϴ� �޼���
    [Command]
    private void CmdSetAnimatorParameters(string animationKey, float speed)
    {
        RpcSetAnimatorParameters(animationKey, speed);
    }

    // Ŭ���̾�Ʈ���� �ִϸ����� �Ű������� �����ϴ� �޼���
    [ClientRpc]
    private void RpcSetAnimatorParameters(string animationKey, float speed)
    {
        if (animator == null)
        {
            Debug.LogWarning("Animator is null. Skipping animation update.");
            return;
        }

        if (!string.IsNullOrEmpty(animationKey))
        {
            animator.Play(animationKey);
        }
        animator.SetFloat("speed", speed);
    }

    private void HandleTargeting()
    {
        float narrowAngle = characterData.narrowAngle;
        float wideAngle = characterData.wideAngle;
        float shortDistance = characterData.shortDistance;
        float longDistance = characterData.longDistance;

        CharacterBehaviour newTarget = default;
        float bestWeight = float.MinValue;
         
        foreach (var enemy in EntityContainer.Instance.CharacterList)
        {
            if (enemy is EnemyController)
            {
                Vector3 toEnemy = enemy.transform.position - transform.position;
                float distance = toEnemy.magnitude;
                float angle = Vector3.Angle(transform.forward, toEnemy);

                // �� ��ä�� �� �ϳ��� ���� ���ԵǸ� Ÿ���� ����
                bool isInNarrowSector = angle <= narrowAngle && distance <= longDistance;
                bool isInWideSector = angle <= wideAngle && distance <= shortDistance;

                if (!isInNarrowSector && !isInWideSector)
                {
                    continue; // �� ��ä�� �� ��� �ʿ��� ���Ե��� ������ �ǳʶ�
                }

                float weight = CalculateTargetWeight(distance, angle, narrowAngle, wideAngle, shortDistance, longDistance);

                if (weight > bestWeight)
                {
                    bestWeight = weight;
                    newTarget = enemy;
                }
            }
        }

        target = newTarget;
    }

    private float CalculateTargetWeight(float distance, float angle, float narrowAngle, float wideAngle, float shortDistance, float longDistance)
    {
        float distanceFactor = 1f - Mathf.Clamp01(distance / longDistance); // �Ÿ��� �������� ����ġ�� ����
        float angleFactor;

        if (angle <= narrowAngle)
        {
            angleFactor = 1f; // ���� ���������� ���� ����ġ
        }
        else if (angle <= wideAngle)
        {
            angleFactor = 1f - Mathf.Clamp01((angle - narrowAngle) / (wideAngle - narrowAngle));
        }
        else
        {
            angleFactor = 0f; // ���� ���� ������ ����� ����ġ ����
        }

        // �����̴� ���� ���� ����ġ�� ���
        float combinedWeight = Mathf.Lerp(distanceFactor, angleFactor, characterData.targetingWeightThreshold);

        return combinedWeight;
    }

    protected override void EndAction()
    {
        base.EndAction();
        if (Input.GetMouseButton(0))
        {
            isTouching = true;
            touchStartTime = Time.time;
            isTapConfirmed = false;
        }
        if (AnimatorHasLayer(animator, 1))
        {
            animator.SetLayerWeight(1, 0);
        }
    }


    ///
    /// ����
    ///



    /// <summary>
    /// �����
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

        // ��ä�� �ð�ȭ
        DrawTargetingGizmos();
    }

    private void DrawTargetingGizmos()
    {
        Vector3 forward = transform.forward;

        // ���� ����/�� �Ÿ� ��ä��
        Gizmos.color = Color.yellow;
        DrawArc(forward, characterData.narrowAngle, characterData.longDistance);

        // ���� ����/ª�� �Ÿ� ��ä��
        Gizmos.color = Color.green;
        DrawArc(forward, characterData.wideAngle, characterData.shortDistance);
    }

    private void DrawArc(Vector3 forward, float angle, float distance)
    {
        int segmentCount = 20; // ��ä���� ���׸�Ʈ ��
        float angleStep = angle / segmentCount;

        Vector3 lastPoint = transform.position + Quaternion.Euler(0, -angle / 2, 0) * forward * distance;

        for (int i = 1; i <= segmentCount; i++)
        {
            float currentAngle = -angle / 2 + angleStep * i;
            Vector3 nextPoint = transform.position + Quaternion.Euler(0, currentAngle, 0) * forward * distance;

            Gizmos.DrawLine(lastPoint, nextPoint);
            lastPoint = nextPoint;
        }

        Gizmos.DrawLine(transform.position, lastPoint);
    }

}
