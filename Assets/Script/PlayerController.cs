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

        // 현재 프레임에서 실제 이동한 속도 계산
        float movementSpeed = (transform.position - lastPosition).magnitude / Time.deltaTime;

        // 캐릭터가 이동 중일 때만 currentSpeed 업데이트
        if (currentState == CharacterState.Move)
        {
            currentSpeed = Mathf.Max(movementSpeed, characterData.moveSpeed); // 최소한의 속도를 보장
        }
        else
        {
            currentSpeed = 0; // 이동하지 않으면 속도를 0으로 설정
        }

        // 현재 위치를 다음 프레임을 위해 저장
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

        // WallCollider 레이어 마스크 설정 (Inspector에서 설정 가능)
        LayerMask wallLayer = LayerMask.GetMask("WallCollider");

        // 첫 번째 충돌을 감지하는 SphereCast
        if (Physics.SphereCast(transform.position, radius, moveDirection, out hit, currentSpeed * Time.deltaTime, wallLayer))
        {
            // 첫 번째 벽의 표면 노멀을 얻는다
            Vector3 normal = hit.normal;

            // 첫 번째 벽을 따라 미끄러질 방향을 계산
            Vector3 slideDirection = Vector3.ProjectOnPlane(moveDirection, normal).normalized;

            // 첫 번째 벽에 대한 감속 처리
            float firstAngle = Vector3.Angle(moveDirection, normal);
            float firstSpeedAdjustment = Mathf.Clamp01(1 - Mathf.Abs(firstAngle - 90) / 90f);

            // 수정된 방향으로 두 번째 충돌을 검사하는 SphereCast
            if (Physics.SphereCast(transform.position, radius, slideDirection, out hit, currentSpeed * Time.deltaTime, wallLayer))
            {
                // 두 번째 벽의 표면 노멀을 얻는다
                Vector3 secondNormal = hit.normal;

                // 두 번째 벽을 따라 미끄러질 방향을 다시 계산
                slideDirection = Vector3.ProjectOnPlane(slideDirection, secondNormal).normalized;

                // 두 번째 벽에 대한 감속 처리
                float secondAngle = Vector3.Angle(slideDirection, secondNormal);
                float secondSpeedAdjustment = Mathf.Clamp01(1 - Mathf.Abs(secondAngle - 90) / 90f);

                // 두 번째 벽에 대한 감속 적용
                firstSpeedAdjustment *= secondSpeedAdjustment;
            }

            // 벽을 따라 미끄러지면서 이동, 감속 적용
            transform.position += slideDirection * currentSpeed * firstSpeedAdjustment * Time.deltaTime;
        }
        else
        {
            // 벽에 닿지 않았을 때는 기본 이동
            transform.position += moveDirection * currentSpeed * Time.deltaTime;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);

        // 이동 속도를 애니메이션에 반영
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

                // 두 부채꼴 중 하나라도 적이 포함되면 타겟팅 가능
                bool isInNarrowSector = angle <= narrowAngle && distance <= longDistance;
                bool isInWideSector = angle <= wideAngle && distance <= shortDistance;

                if (!isInNarrowSector && !isInWideSector)
                {
                    continue; // 두 부채꼴 중 어느 쪽에도 포함되지 않으면 건너뜀
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
        float distanceFactor = 1f - Mathf.Clamp01(distance / longDistance); // 거리가 가까울수록 가중치가 높음
        float angleFactor;

        if (angle <= narrowAngle)
        {
            angleFactor = 1f; // 좁은 각도에서는 높은 가중치
        }
        else if (angle <= wideAngle)
        {
            angleFactor = 1f - Mathf.Clamp01((angle - narrowAngle) / (wideAngle - narrowAngle));
        }
        else
        {
            angleFactor = 0f; // 넓은 각도 범위를 벗어나면 가중치 없음
        }

        // 슬라이더 값에 따라 가중치를 계산
        float combinedWeight = Mathf.Lerp(distanceFactor, angleFactor, characterData.targetingWeightThreshold);

        return combinedWeight;
    }

    /// AI관련
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
        // 가장 먼저 타겟이 잡힌 적을 타겟으로 설정하고 공격
        if (target == null)
        {
            HandleTargeting();
        }
        ProcessInputMessage(InputMessage.A);
    }

    private void HandleCooperativeAI()
    {
        // 리더가 공격하는 타겟을 같이 공격
        if (target == null && EntityContainer.Instance.LeaderPlayer.target != null)
        {
            target = EntityContainer.Instance.LeaderPlayer.target;
        }
        ProcessInputMessage(InputMessage.A);
    }

    private void HandleSupportiveAI()
    {
        // 플레이어를 따라다니기만 함
        if (target == null)
        {
            FollowLeader();
        }
    }

    private void HandleVanguardAI()
    {
        // 플레이어보다 앞서 몬스터를 탐색
        if (target == null)
        {
            Vector3 forwardPosition = EntityContainer.Instance.LeaderPlayer.transform.position + EntityContainer.Instance.LeaderPlayer.transform.forward * 5f;
            transform.position = Vector3.Lerp(transform.position, forwardPosition, Time.deltaTime * 2f);

            // 리더나 자신이 피격된 경우 공격
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
    /// 기즈모
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

        // 부채꼴 시각화
        DrawTargetingGizmos();
    }

    private void DrawTargetingGizmos()
    {
        Vector3 forward = transform.forward;

        // 좁은 각도/긴 거리 부채꼴
        Gizmos.color = Color.yellow;
        DrawArc(forward, characterData.narrowAngle, characterData.longDistance);

        // 넓은 각도/짧은 거리 부채꼴
        Gizmos.color = Color.green;
        DrawArc(forward, characterData.wideAngle, characterData.shortDistance);
    }

    private void DrawArc(Vector3 forward, float angle, float distance)
    {
        int segmentCount = 20; // 부채꼴의 세그먼트 수
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
