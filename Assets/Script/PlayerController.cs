using UnityEngine;

public class PlayerController : CharacterBehaviour
{
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
    }

    protected override void Update()
    {
        if (currentState is CharacterState.Idle or CharacterState.Move)
            HandleInput();

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
}
