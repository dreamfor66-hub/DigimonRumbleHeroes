using System.Linq;
using UnityEngine;

public class PlayerController : CharacterBehaviour
{
    private Vector3 targetPosition;
    private Vector3 lastPosition;
    private bool isTouching = false;
    private Vector3 initialTouchPosition;
    private float touchStartTime;
    private bool isTapConfirmed = false;

    public bool isLeader;
    public AIType aiType;

    protected override void Start()
    {
        base.Start();
        lastPosition = transform.position;

        if (isLeader)
        {
            EntityContainer.Instance.LeaderPlayer = this;
        }
    }

    protected override void Update()
    {
        HandleTargeting();

        if (currentState is CharacterState.Idle or CharacterState.Move & isLeader)
        {
            HandleInput();
        }

        if (!isLeader)
        {
            HandleAI();
        }

        base.Update();

        // ���� �����ӿ��� ���� �̵��� �ӵ� ���
        float movementSpeed = (transform.position - lastPosition).magnitude / Time.deltaTime;

        // ĳ���Ͱ� �̵� ���� ���� currentSpeed ������Ʈ
        if (currentState == CharacterState.Move)
        {
            currentSpeed = Mathf.Max(movementSpeed, characterData.moveSpeed); // �ּ����� �ӵ��� ����
        }
        else
        {
            currentSpeed = 0; // �̵����� ������ �ӵ��� 0���� ����
        }

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

        Vector3 touchDelta = position - initialTouchPosition;
        float distance = touchDelta.magnitude;
        float elapsedTime = Time.time - touchStartTime;

        if (elapsedTime > PlayerSettings.Instance.tapThreshold || distance >= PlayerSettings.Instance.dragThreshold)
        {
            isTapConfirmed = false;

            float speedMultiplier = Mathf.Clamp(distance / PlayerSettings.Instance.maxDistance, 0f, 1f);

            Vector3 inputDirection = new Vector3(touchDelta.x, 0, touchDelta.y).normalized;

            Vector3 cameraForward = Camera.main.transform.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();

            Vector3 cameraRight = Camera.main.transform.right;
            cameraRight.y = 0;
            cameraRight.Normalize();

            direction = inputDirection.x * cameraRight + inputDirection.z * cameraForward;

            currentSpeed = characterData.moveSpeed * speedMultiplier;

            currentState = CharacterState.Move;
        }
        else
        {
            currentState = CharacterState.Idle;
            currentSpeed = 0;
        }
    }

    private void EndTouch(Vector3 position)
    {
        if (!isTouching)
            return;

        Vector3 touchDelta = position - initialTouchPosition;
        float distance = touchDelta.magnitude;
        float elapsedTime = Time.time - touchStartTime;

        if (distance < PlayerSettings.Instance.dragThreshold && elapsedTime < PlayerSettings.Instance.tapThreshold)
        {
            HandleInputMessage(InputMessage.A);
        }
        else
        {
            currentState = CharacterState.Idle;
        }

        isTouching = false;
        stopTimer = stopTime;
    }


protected override void HandleIdle()
    {
        if (stopTimer > 0)
        {
            currentSpeed = Mathf.Lerp(currentSpeed, 0, (stopTime - stopTimer) / stopTime);
            stopTimer -= Time.deltaTime;

            if (stopTimer <= 0)
            {
                animator.SetBool("isMoving", false);
                currentSpeed = 0;
            }
            MoveCharacter();
        }
        animator.Play("Idle");
        animator.SetFloat("speed", 0f, 0.15f, Time.deltaTime);
    }

    protected override void HandleMovement()
    {
        RaycastHit hit;

        var radius = characterData.colliderRadius;
        var moveDirection = direction.normalized;

        // WallCollider ���̾� ����ũ ���� (Inspector���� ���� ����)
        LayerMask wallLayer = LayerMask.GetMask("WallCollider");

        // ù ��° �浹�� �����ϴ� SphereCast
        if (Physics.SphereCast(transform.position, radius, moveDirection, out hit, currentSpeed * Time.deltaTime, wallLayer))
        {
            // ù ��° ���� ǥ�� ����� ��´�
            Vector3 normal = hit.normal;

            // ù ��° ���� ���� �̲����� ������ ���
            Vector3 slideDirection = Vector3.ProjectOnPlane(moveDirection, normal).normalized;

            // ù ��° ���� ���� ���� ó��
            float firstAngle = Vector3.Angle(moveDirection, normal);
            float firstSpeedAdjustment = Mathf.Clamp01(1 - Mathf.Abs(firstAngle - 90) / 90f);

            // ������ �������� �� ��° �浹�� �˻��ϴ� SphereCast
            if (Physics.SphereCast(transform.position, radius, slideDirection, out hit, currentSpeed * Time.deltaTime, wallLayer))
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
            transform.position += slideDirection * currentSpeed * firstSpeedAdjustment * Time.deltaTime;
        }
        else
        {
            // ���� ���� �ʾ��� ���� �⺻ �̵�
            transform.position += moveDirection * currentSpeed * Time.deltaTime;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);

        // �̵� �ӵ��� �ִϸ��̼ǿ� �ݿ�
        float normalizedSpeed = currentSpeed / characterData.moveSpeed;
        animator.Play("Idle");
        animator.SetFloat("speed", normalizedSpeed);
    }

    private void HandleTargeting()
    {
        float narrowAngle = characterData.narrowAngle;
        float wideAngle = characterData.wideAngle;
        float shortDistance = characterData.shortDistance;
        float longDistance = characterData.longDistance;

        CharacterBehaviour newTarget = null;
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

    /// AI����
    private void HandleAI()
    {
        switch (aiType)
        {
            case AIType.Aggressive:
                HandleAggressiveAI();
                break;
            case AIType.Cooperative:
                HandleCooperativeAI();
                break;
            case AIType.Supportive:
                HandleSupportiveAI();
                break;
            case AIType.Vanguard:
                HandleVanguardAI();
                break;
        }
    }

    private void HandleAggressiveAI()
    {
        // ���� ���� Ÿ���� ���� ���� Ÿ������ �����ϰ� ����
        if (target == null)
        {
            HandleTargeting();
        }
        ProcessInputMessage(InputMessage.A);
    }

    private void HandleCooperativeAI()
    {
        // ������ �����ϴ� Ÿ���� ���� ����
        if (target == null && EntityContainer.Instance.LeaderPlayer.target != null)
        {
            target = EntityContainer.Instance.LeaderPlayer.target;
        }
        ProcessInputMessage(InputMessage.A);
    }

    private void HandleSupportiveAI()
    {
        // �÷��̾ ����ٴϱ⸸ ��
        if (target == null)
        {
            FollowLeader();
        }
    }

    private void HandleVanguardAI()
    {
        // �÷��̾�� �ռ� ���͸� Ž��
        if (target == null)
        {
            Vector3 forwardPosition = EntityContainer.Instance.LeaderPlayer.transform.position + EntityContainer.Instance.LeaderPlayer.transform.forward * 5f;
            transform.position = Vector3.Lerp(transform.position, forwardPosition, Time.deltaTime * 2f);

            // ������ �ڽ��� �ǰݵ� ��� ����
            if (EntityContainer.Instance.LeaderPlayer.target != null)
            {
                target = EntityContainer.Instance.LeaderPlayer.target;
                ProcessInputMessage(InputMessage.A);
            }
        }
    }

    private void FollowLeader()
    {
        Vector3 followPosition = EntityContainer.Instance.LeaderPlayer.transform.position - EntityContainer.Instance.LeaderPlayer.transform.forward * 2f;
        transform.position = Vector3.Lerp(transform.position, followPosition, Time.deltaTime * 2f);
    }


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
