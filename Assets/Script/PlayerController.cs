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
}
