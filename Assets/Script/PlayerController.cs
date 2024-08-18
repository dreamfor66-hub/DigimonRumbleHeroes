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
    public int playerNumber;  // ���� �÷��̾� �ѹ� (1, 2, 3)
    public float followDistance = 2;

    protected override void Start()
    {
        base.Start();
        lastPosition = transform.position;

        EntityContainer.Instance.RegisterPlayer(this);

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

        base.Update();

        if (!isLeader)
        {
            HandleAI();
        }

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
        }
        animator.Play("Idle");
        animator.SetFloat("speed", 0f, 0.15f, Time.deltaTime);
    }

    protected override void HandleMovement()
    {
        Vector3 moveVector = HandleCollisionAndSliding(direction.normalized, currentSpeed);
        transform.position += moveVector;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);

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
        FollowLeader();
        
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
        var leader = EntityContainer.Instance.LeaderPlayer;

        if (leader != null)
        {
            // �⺻ ������
            float baseFollowDistance = 0.4f; //1.2-0.4-0.4
            float angleOffset = 60f;

            // Leader�� AI�� ũ�⸦ ����� followDistance ���
            float leaderRadius = leader.characterData.colliderRadius;
            float thisRadius = this.characterData.colliderRadius;

            float followDistance = baseFollowDistance + leaderRadius + thisRadius;

            // Leader�� AI�� ũ�⸦ ����� �� ����Ʈ ���
            Vector3 leaderPosition = leader.transform.position;
            Vector3 leaderDirection = leader.transform.forward;

            Vector3 targetPosition1 = leaderPosition - Quaternion.Euler(0, angleOffset, 0) * leaderDirection * followDistance;
            Vector3 targetPosition2 = leaderPosition - Quaternion.Euler(0, -angleOffset, 0) * leaderDirection * followDistance;

            // �ٸ� AI ĳ������ ��ġ Ȯ��
            var otherAI = EntityContainer.Instance.PlayerList.FirstOrDefault(ai => ai != this && !ai.isLeader);

            Vector3 selectedTarget;

            if (otherAI != null)
            {
                float otherAIRadius = otherAI.characterData.colliderRadius;

                float distanceToTarget1 = Vector3.Distance(transform.position, targetPosition1);
                float distanceToTarget2 = Vector3.Distance(transform.position, targetPosition2);

                float otherAIDistanceToTarget1 = Vector3.Distance(otherAI.transform.position, targetPosition1);
                float otherAIDistanceToTarget2 = Vector3.Distance(otherAI.transform.position, targetPosition2);

                if (otherAIDistanceToTarget1 < followDistance)
                {
                    selectedTarget = targetPosition2;
                }
                else if (otherAIDistanceToTarget2 < followDistance)
                {
                    selectedTarget = targetPosition1;
                }
                else
                {
                    selectedTarget = distanceToTarget1 < distanceToTarget2 ? targetPosition1 : targetPosition2;
                }

                // �� AI�� colliderRadius�� ���Ͽ� combinedRadius ���
                float combinedRadius = thisRadius + otherAIRadius;

                // �ٸ� AI���� �浹�� ���ϱ� ���� �ݴ� �������� �̵� (ȸ�� ����)
                float otherAIDistance = Vector3.Distance(transform.position, otherAI.transform.position);

                if (otherAIDistance < combinedRadius)
                {
                    Vector3 avoidanceDirection = (transform.position - otherAI.transform.position).normalized;
                    selectedTarget += avoidanceDirection * (combinedRadius - otherAIDistance);
                }
            }
            else
            {
                // �ٸ� AI�� ���� ���, ����� ��ǥ ����Ʈ ����
                selectedTarget = Vector3.Distance(transform.position, targetPosition1) < Vector3.Distance(transform.position, targetPosition2) ? targetPosition1 : targetPosition2;
            }

            Vector3 directionToTarget = (selectedTarget - transform.position).normalized;
            float distanceToSelectedTarget = Vector3.Distance(transform.position, selectedTarget);

            if (distanceToSelectedTarget > followDistance || (otherAI != null && Vector3.Distance(transform.position, otherAI.transform.position) < thisRadius + otherAI.characterData.colliderRadius))
            {
                Vector3 finalMoveVector = HandleCollisionAndSliding(directionToTarget, characterData.moveSpeed);
                transform.position += finalMoveVector;

                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);

                // �ִϸ��̼� �ӵ� �ݿ�
                float movementSpeed = finalMoveVector.magnitude / Time.deltaTime;
                float normalizedSpeed = movementSpeed / characterData.moveSpeed;
                animator.SetFloat("speed", normalizedSpeed, 0.035f, Time.deltaTime);
            }
            else
            {
                Vector3 finalMoveVector = directionToTarget * currentSpeed * Time.deltaTime;
                transform.position += finalMoveVector;

                // �ִϸ��̼� �ӵ��� ���ӽ�Ű�� 0���� ����
                animator.SetFloat("speed", 0f, 0.75f, Time.deltaTime);
            }
        }
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
