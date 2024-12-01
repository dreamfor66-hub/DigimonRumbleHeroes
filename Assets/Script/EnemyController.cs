using Sirenix.OdinInspector;
using UnityEngine;
using Mirror;

public class EnemyController : CharacterBehaviour
{
    public BotAIData botAIData;
    [SyncVar, Mirror.ShowInInspector]
    private bool isAware = false;
    [SyncVar, Mirror.ShowInInspector]
    private float stateTimer = 0f;
    [SyncVar, Mirror.ShowInInspector]
    private int currentBotAIIndex = 0;
    [SyncVar, Mirror.ShowInInspector]
    private float duration = 0f;

    public bool isSuperArmor = false;

    protected override void Start()
    {
        base.Start();
        teamType = TeamType.Enemy;
        currentSpeed = characterData.moveSpeed;
        ChangeStatePrev(CharacterState.Init);
        if (isSuperArmor)
            CmdAddStatus(StatusType.SuperArmor, -1f); // -1로 영구 슈퍼아머 상태 추가
        FindClosestPlayer();
        UpdateCurrentAIState();

            
    }

    protected override void Update()
    {
        if (isDie)
            return;
        if (target == null)
            FindClosestPlayer();

        UpdateCooldownTimers();
        base.Update();

        if (currentState != CharacterState.Action)
        {
            stateTimer += Time.deltaTime;
        }

        if (currentState == CharacterState.Init)
        {
            HandleAwareness();
            if (isAware)
            {
                TransitionToNextState();
            }
            return;
        }

        if (stateTimer >= duration)
        {
            TransitionToNextState();
        }

        HandleAwareness();
    }

    protected override void EndAction()
    {
        base.EndAction();
        ChangeStatePrev(CharacterState.Init);
        stateTimer = 0f;
        TransitionToNextState();
    }

    private void TransitionToNextState()
    {
        if (target == null)
        {
            FindClosestPlayer();
        }
        float distanceToPlayer = Vector3.Distance(transform.position, target.transform.position);

        for (int i = 0; i < botAIData.botAIStates.Count; i++)
        {
            var state = botAIData.botAIStates[i];

            if (state.state == currentState && distanceToPlayer >= state.distanceRange.x && distanceToPlayer <= state.distanceRange.y)
            {
                // 쿨다운 상태 확인
                if (state.nextState == CharacterState.Action && state.IsOnCooldown())
                {
                    continue; // 쿨다운 중이라면 다음 상태로 넘어감
                }
                currentState = state.nextState;
                currentBotAIIndex = i;
                UpdateCurrentAIState();

                stateTimer = 0f;

                if (currentState == CharacterState.Action)
                {
                    StartAction(state.actionKey);
                    state.cooldownTimer = state.cooldown;
                }
                return;
            }
        }
    }

    private void UpdateCurrentAIState()
    {
        BotAIState currentAIState = botAIData.botAIStates[currentBotAIIndex];
        duration = currentAIState.duration;
    }
    // BotAIState의 쿨다운 타이머를 개별적으로 업데이트
    private void UpdateCooldownTimers()
    {
        foreach (var state in botAIData.botAIStates)
        {
            state.UpdateCooldown(Time.deltaTime);
        }
    }
    private void HandleAwareness()
    {


        float distanceToAnyPlayer = float.MaxValue;
        CharacterBehaviour awarePlayer = null;

        foreach (var player in EntityContainer.Instance.PlayerList)
        {
            if (player == null || player.gameObject == null) continue;
            distanceToAnyPlayer = Vector3.Distance(transform.position, player.transform.position);
            awarePlayer = player;
        }

        if (!isAware && distanceToAnyPlayer <= characterData.attackRange && awarePlayer != null)
        {
            target = awarePlayer;
            isAware = true;
            if (currentState == CharacterState.Init)
            {
                TransitionToNextState();
            }
        }

    }

    protected override void HandleIdle()
    {
        base.HandleIdle();
        speedMultiplier = 0;
        currentSpeed = characterData.moveSpeed * speedMultiplier;
        animator.Play("Idle");
        float normalizedSpeed = 0; // 항상 1로 유지
        animator.SetFloat("speed", normalizedSpeed);
        LookAtPlayer();
    }

    protected override void HandleMovement()
    {
        base.HandleMovement();
        if (target == null) return;

        speedMultiplier = 1;
        currentSpeed = characterData.moveSpeed * speedMultiplier;

        Vector3 directionToTarget = (target.transform.position - transform.position).normalized;

        // 충돌과 슬라이딩을 처리한 움직임 계산
        Vector3 moveVector = HandleCollisionAndSliding(directionToTarget, currentSpeed);
        transform.position += moveVector;

        // 높이를 0으로 고정
        transform.position = new Vector3(transform.position.x, 0, transform.position.z);

        // 방향 설정 및 회전
        if (directionToTarget != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }

        // 애니메이션 처리
        animator.Play("Idle");
        float normalizedSpeed = 1; // 항상 1로 유지
        animator.SetFloat("speed", normalizedSpeed);

        //// 네트워크 동기화 (로컬 플레이어일 때만)
        //if (isLocalPlayer)
        //{
        //    CmdSetAnimatorParameters("Move", normalizedSpeed);
        //}
    }

    private void FindClosestPlayer()
    {
        float closestDistance = float.MaxValue;
        PlayerController closestPlayer = null;

        foreach (var character in EntityContainer.Instance.CharacterList)
        {
            if (character is PlayerController player)
            {
                if (player == null) // 플레이어가 null인지 확인
                    continue;

                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlayer = player;
                }
            }
        }

        target = closestPlayer; // 가장 가까운 플레이어를 타겟으로 설정
    }

    private void LookAtPlayer()
    {
        if (target == null)
            return;

        Vector3 directionToTarget = (target.transform.position - transform.position).normalized;

        // directionToTarget Vector3.zero인지 확인하고, 아니라면 회전 처리
        if (directionToTarget != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }
}
