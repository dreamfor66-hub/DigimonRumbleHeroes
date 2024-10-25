using System.Collections;
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
    public AIState currentAIState = AIState.Follow; // 기본 상태는 Follow
    [HideInInspector]
    public int playerNumber;  // 고유 플레이어 넘버 (1, 2, 3)

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
        if (isLeader)
            HandleTargeting();

        if (currentState is CharacterState.Idle or CharacterState.Move & isLeader)
        {
            HandleInput();
        }


        if (currentState is CharacterState.Idle or CharacterState.Move & !isLeader)
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

        if (elapsedTime > ResourceHolder.Instance.gameVariables.tapThreshold || distance >= ResourceHolder.Instance.gameVariables.dragThreshold)
        {
            isTapConfirmed = false;

            float speedMultiplier = Mathf.Clamp(distance / ResourceHolder.Instance.gameVariables.maxDistance, 0f, 1f);

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

        if (distance < ResourceHolder.Instance.gameVariables.dragThreshold && elapsedTime < ResourceHolder.Instance.gameVariables.tapThreshold)
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
        if (isLeader)
        {
            animator.Play("Idle");
            animator.SetFloat("speed", 0f, 0.15f, Time.deltaTime);
        }
    }

    protected override void HandleMovement()
    {
        Vector3 moveVector = HandleCollisionAndSliding(direction.normalized, currentSpeed);
        transform.position += moveVector;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);

        float normalizedSpeed = currentSpeed / characterData.moveSpeed;

        if (isLeader)
        {
            animator.Play("Idle");
            animator.SetFloat("speed", normalizedSpeed);
        }
    }

    private void HandleTargeting()
    {
        float narrowAngle = characterData.narrowAngle;
        float wideAngle = characterData.wideAngle;
        float shortDistance = characterData.shortDistance;
        float longDistance = characterData.longDistance;

        CharacterBehaviour newTarget = this;
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
            //case AIType.Vanguard:
            //    HandleVanguardAI();
            //    break;
        }

        // 상태에 따른 행동 처리
        switch (currentAIState)
        {
            case AIState.Follow:
                FollowLeader();
                break;
            case AIState.ForceFollow:
                FollowLeader(isForce: true);
                break;
            case AIState.MoveToward:
                MoveTowardsTarget();
                break;
            case AIState.Attack:
                ExecuteAttack();
                break;
        }
    }

    private float moveTowardTimer = 0f; // MoveToward 상태에서의 시간을 추적하는 타이머
    private const float moveTowardDuration = 2f; // MoveToward 상태에서 유지되는 시간 (2초)

    private void HandleAggressiveAI()
    {
        var forceFollowDistance = 2.5f;
        var followDistance = 0.4f;
        float leaderRadius = EntityContainer.Instance.LeaderPlayer.characterData.colliderRadius;
        float thisRadius = this.characterData.colliderRadius;

        // 리더와의 거리 체크
        if (currentAIState == AIState.MoveToward && Vector3.Distance(transform.position, EntityContainer.Instance.LeaderPlayer.transform.position) > forceFollowDistance + leaderRadius + thisRadius)
        {
            if (moveTowardTimer < moveTowardDuration)
            {
                moveTowardTimer += Time.deltaTime; // MoveToward 상태에서 시간을 추가
            }
            else
            {
                currentAIState = AIState.ForceFollow; // 타이머가 만료되면 ForceFollow로 전환
                moveTowardTimer = 0f; // 타이머 초기화
                return;
            }
        }
        else if (currentAIState == AIState.ForceFollow && Vector3.Distance(transform.position, EntityContainer.Instance.LeaderPlayer.transform.position) <= (followDistance + leaderRadius + thisRadius))
        {
            currentAIState = AIState.Follow; // 플레이어와 충분히 가까워졌으면 Follow로 전환
            moveTowardTimer = 0f; // 타이머 초기화
            return;
        }
        else if (currentAIState == AIState.MoveToward)
        {
            // 리더와의 거리가 다시 가까워지면 타이머 초기화
            if (Vector3.Distance(transform.position, EntityContainer.Instance.LeaderPlayer.transform.position) <= forceFollowDistance)
            {
                moveTowardTimer = 0f;
            }
        }

        if (target == null || currentAIState == AIState.Follow)
        {
            HandleTargeting(); // 새로운 타겟을 찾음
            if (target == null)
            {
                currentAIState = AIState.Follow; // 타겟이 없으면 리더를 따름
                return;
            }
        }

        // 타겟이 있을 때만 MoveToward 또는 Attack 상태로 전환
        if (currentAIState != AIState.ForceFollow)
        {
            Vector3 directionToTarget = (target.transform.position - transform.position).normalized;
            float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);

            if (distanceToTarget <= characterData.attackRange || distanceToTarget <= (characterData.colliderRadius + target.characterData.colliderRadius))
            {
                currentAIState = AIState.Attack; // 공격 범위 내에 타겟이 있으면 공격
            }
            else if (currentAIState == AIState.MoveToward || currentAIState == AIState.Attack)
            {
                currentAIState = AIState.MoveToward; // 타겟이 멀리 있으면 접근 (타이머 유지)
            }
            else
            {
                currentAIState = AIState.MoveToward; // 타겟이 멀리 있으면 접근 (타이머 새로 시작)
                moveTowardTimer = 0f; // 새로운 MoveToward 상태 시작 시 타이머 초기화
            }
        }
    }

    private void MoveTowardsTarget()
    {
        Vector3 directionToTarget = (target.transform.position - transform.position).normalized;
        Vector3 finalMoveVector = HandleCollisionAndSliding(directionToTarget, characterData.moveSpeed);
        transform.position += finalMoveVector;

        float movementSpeed = finalMoveVector.magnitude / Time.deltaTime;
        float normalizedSpeed = movementSpeed / characterData.moveSpeed;
        animator.Play("Idle");
        animator.SetFloat("speed", normalizedSpeed, 0.035f, Time.deltaTime);

        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);

        // 애니메이션 및 상태 전환 처리
        float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
        if (distanceToTarget <= characterData.attackRange || distanceToTarget <= (characterData.colliderRadius + target.characterData.colliderRadius))
        {
            currentAIState = AIState.Attack;
        }
    }

    private void ExecuteAttack()
    {
        ProcessInputMessage(InputMessage.A); // 공격 실행
                                             // 공격 후 다시 상태를 판단하여 전환할 수 있음
    }


    private void HandleCooperativeAI()
    {
        var leader = EntityContainer.Instance.LeaderPlayer;

        var forceFollowDistance = 1.5f;
        var followDistance = 0.4f;
        float leaderRadius = leader.characterData.colliderRadius;
        float thisRadius = this.characterData.colliderRadius;

        // 리더와의 거리 체크
        if (currentAIState == AIState.MoveToward && Vector3.Distance(transform.position, leader.transform.position) > forceFollowDistance + leaderRadius + thisRadius)
        {
            if (moveTowardTimer >= moveTowardDuration)
            {
                currentAIState = AIState.ForceFollow; // 타이머가 만료되면 ForceFollow로 전환
                return;
            }
        }
        else if (currentAIState == AIState.ForceFollow && Vector3.Distance(transform.position, leader.transform.position) <= (followDistance + leaderRadius + thisRadius))
        {
            currentAIState = AIState.Follow; // 플레이어와 충분히 가까워졌으면 Follow로 전환
            return;
        }
        if (leader != null && leader.hit)
        {
            target = leader.currentHitTarget;
            if (target != null)
                currentAIState = AIState.MoveToward; // 타겟이 설정되었으면 MoveToward 상태로 전환

        }
        else if (leader != null && leader.attacked)
        {
            target = leader.currentAttacker;
            if (target != null)
                currentAIState = AIState.MoveToward; // 타겟이 설정되었으면 MoveToward 상태로 전환

        }
        else if (lastAttacker != null)
        {
            target = lastAttacker;
            if (target != null)
                currentAIState = AIState.MoveToward; // 타겟이 설정되었으면 MoveToward 상태로 전환
        }

        if (target == null)
        {
            if (leader.lastAttacker != null)
            {
                target = leader.lastAttacker;
            }
            else if (leader.lastHitTarget != null)
            {
                target = leader.lastHitTarget;
            }
            else
            {
                currentAIState = AIState.Follow;
            }
        }
        else if (currentAIState != AIState.ForceFollow)
        {
            
            Vector3 directionToTarget = (target.transform.position - transform.position).normalized;
            float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);

            if (distanceToTarget <= characterData.attackRange || distanceToTarget <= (characterData.colliderRadius + target.characterData.colliderRadius))
            {
                currentAIState = AIState.Attack; // 공격 범위 내에 타겟이 있으면 공격
            }
            else if (currentAIState == AIState.MoveToward || currentAIState == AIState.Attack)
            {
                currentAIState = AIState.MoveToward; // 타겟이 멀리 있으면 접근 (타이머 유지)
            }
            else
            {
                currentAIState = AIState.MoveToward; // 타겟이 멀리 있으면 접근 (타이머 새로 시작)
            }
            
        }
    }

    private void HandleSupportiveAI()
    {
        // 플레이어를 따라다니기만 함
        currentAIState = AIState.ForceFollow;
        
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

    private void FollowLeader(bool isForce = false)
    {
        var leader = EntityContainer.Instance.LeaderPlayer;

        if (leader != null)
        {
            // 기본 설정값
            float baseFollowDistance = 0.4f;
            float angleOffset = 60f;

            // Leader와 AI의 크기를 고려한 followDistance 계산
            float leaderRadius = leader.characterData.colliderRadius;
            float thisRadius = this.characterData.colliderRadius;

            float followDistance = baseFollowDistance + leaderRadius + thisRadius;

            // Leader와 AI의 크기를 고려한 각 포인트 계산
            Vector3 leaderPosition = leader.transform.position;
            Vector3 leaderDirection = leader.transform.forward;

            Vector3 targetPosition1 = leaderPosition - Quaternion.Euler(0, angleOffset, 0) * leaderDirection * followDistance;
            Vector3 targetPosition2 = leaderPosition - Quaternion.Euler(0, -angleOffset, 0) * leaderDirection * followDistance;

            // 다른 AI 캐릭터의 위치 확인
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

                // 두 AI의 colliderRadius를 더하여 combinedRadius 계산
                float combinedRadius = thisRadius + otherAIRadius;

                // 다른 AI와의 충돌을 피하기 위해 반대 방향으로 이동 (회피 로직)
                float otherAIDistance = Vector3.Distance(transform.position, otherAI.transform.position);

                if (otherAIDistance < combinedRadius)
                {
                    Vector3 avoidanceDirection = (transform.position - otherAI.transform.position).normalized;
                    selectedTarget += avoidanceDirection * (combinedRadius - otherAIDistance);
                }
            }
            else
            {
                // 다른 AI가 없는 경우, 가까운 목표 포인트 선택
                selectedTarget = Vector3.Distance(transform.position, targetPosition1) < Vector3.Distance(transform.position, targetPosition2) ? targetPosition1 : targetPosition2;
            }

            Vector3 directionToTarget = (selectedTarget - transform.position).normalized;
            float distanceToSelectedTarget = Vector3.Distance(transform.position, selectedTarget);

            // 강제 이동 또는 일반 이동 처리
            if (isForce || distanceToSelectedTarget > followDistance || (otherAI != null && Vector3.Distance(transform.position, otherAI.transform.position) < thisRadius + otherAI.characterData.colliderRadius))
            {
                Vector3 finalMoveVector = HandleCollisionAndSliding(directionToTarget, characterData.moveSpeed);
                transform.position += finalMoveVector;

                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);

                // 애니메이션 속도 반영
                float movementSpeed = finalMoveVector.magnitude / Time.deltaTime;
                float normalizedSpeed = movementSpeed / characterData.moveSpeed;
                animator.Play("Idle");
                animator.SetFloat("speed", normalizedSpeed, 0.035f, Time.deltaTime);
            }
            else
            {
                // Follow 상태에서 가까워지면 속도를 줄이고 자유롭게 상태 전환
                Vector3 finalMoveVector = directionToTarget * currentSpeed * Time.deltaTime;
                transform.position += finalMoveVector;

                // 애니메이션 속도를 감속시키며 0으로 변경
                animator.Play("Idle");
                animator.SetFloat("speed", 0f, 0.15f, Time.deltaTime);
            }
        }
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
