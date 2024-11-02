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
    private Vector3 initialTouchPosition;
    private float touchStartTime;
    private bool isTapConfirmed = false;


    protected override void Start()
    {
        base.Start();
        lastPosition = transform.position;

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
        //if (isLeader)
        HandleTargeting();

        if (currentState is CharacterState.Idle or CharacterState.Move/* & isLeader*/)
        {
            HandleInput();
        }

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

            CmdChangeState(CharacterState.Move);
        }
        else
        {
            CmdChangeState(CharacterState.Idle);
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
            CmdChangeState(CharacterState.Idle);
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
        Vector3 moveVector = HandleCollisionAndSliding(direction.normalized, currentSpeed);
        transform.position += moveVector;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
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
