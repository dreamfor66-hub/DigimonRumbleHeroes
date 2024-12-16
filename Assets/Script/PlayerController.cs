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
    //줄어들 수록 좋은 float. 1이면 1초에 1번 발사, 0.5면 0.5초에 1번 발사 이런 식
    float basicAttackCycle;

    [SyncVar]
    float basicTimer = 0f;

    [SerializeField]
    [SyncVar]
    //Update를 돌다가, 만약 idle이거나 Move인 동시에 basicReady라면 basic01 어택 실행 시키는 것을 감지하기
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
        // 로컬 플레이어 전용 카메라 설정
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

        // 기본 공격 주기 처리
        basicTimer += Time.deltaTime;
        if (basicTimer >= basicAttackCycle)
        {
            basicReady = true;
        }

        // Idle 또는 Move 상태일 때, 조건에 맞으면 기본 공격 실행
        if (basicReady && (currentState == CharacterState.Idle || currentState == CharacterState.Move))
        {
            if (target != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
                if (distanceToTarget <= characterData.attackRange)
                {
                    ReceiveInputMessage(InputMessage.A);
                    basicTimer = 0f; // 타이머 초기화
                    basicReady = false; // 다시 대기 상태로 변경
                }
            }
        }

        //// 현재 프레임에서 실제 이동한 속도 계산
        float movementSpeed = (transform.position - lastPosition).magnitude / Time.deltaTime;

        //// 캐릭터가 이동 중일 때만 currentSpeed 업데이트
        //if (currentState == CharacterState.Move)
        //{
        //    currentSpeed = Mathf.Clamp(movementSpeed, , characterData.moveSpeed); // 최소한의 속도를 보장
        //}
        //else
        //{
        //    currentSpeed = 0; // 이동하지 않으면 속도를 0으로 설정
        //}

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

        // 기존 로직 유지
        animator.Play("Idle");
        animator.SetFloat("speed", 0f, 0.15f, Time.deltaTime);

        // 네트워크 동기화 부분 추가 (로컬 플레이어일 때만 서버에 명령)
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

        // 기존 로직 유지
        animator.Play("Idle");
        animator.SetFloat("speed", normalizedSpeed);

        // 네트워크 동기화 부분 추가 (로컬 플레이어일 때만 서버에 명령)
        if (isLocalPlayer)
        {
            CmdSetAnimatorParameters("Idle", normalizedSpeed);
        }
    }

    public override void StartAction(ActionKey actionKey)
    {
        base.StartAction(actionKey);
        if (currentActionKey == ActionKey.Basic01)
            basicTimer = 0f; // 타이머 초기화
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



    // 서버에 애니메이터 매개변수를 설정하는 메서드
    [Command]
    private void CmdSetAnimatorParameters(string animationKey, float speed)
    {
        RpcSetAnimatorParameters(animationKey, speed);
    }

    // 클라이언트에서 애니메이터 매개변수를 설정하는 메서드
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
    /// 서버
    ///



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
