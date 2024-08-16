using Sirenix.OdinInspector;
using UnityEngine;

public class EnemyController : CharacterBehaviour
{
    public BotAIData botAIData;
    private Transform player;
    [ShowInInspector]
    private bool isAware = false;
    [ShowInInspector]
    private float stateTimer = 0f;
    private int currentBotAIIndex = 0;
    private float duration = 0f;

    protected override void Start()
    {
        base.Start();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        currentState = CharacterState.Init;
        UpdateCurrentAIState();
    }

    protected override void Update()
    {
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

        if (currentState == CharacterState.Action)
        {
            HandleAction();
            return;
        }

        if (stateTimer >= duration)
        {
            TransitionToNextState();
        }

        HandleCurrentState();
        HandleAwareness();
    }

    protected override void EndAction()
    {
        base.EndAction();
        currentState = CharacterState.Init;
        stateTimer = 0f;
        TransitionToNextState();
    }

    private void TransitionToNextState()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        for (int i = 0; i < botAIData.botAIStates.Count; i++)
        {
            var state = botAIData.botAIStates[i];

            if (state.state == currentState && distanceToPlayer >= state.distanceRange.x && distanceToPlayer <= state.distanceRange.y)
            {
                currentState = state.nextState;
                currentBotAIIndex = i;
                UpdateCurrentAIState();

                stateTimer = 0f;

                if (currentState == CharacterState.Action)
                {
                    StartAction(state.actionKey);
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

    private void HandleAwareness()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (!isAware && distanceToPlayer <= characterData.attackRange)
        {
            isAware = true;
            if (currentState == CharacterState.Init)
            {
                TransitionToNextState();
            }
        }
    }

    private void HandleCurrentState()
    {
        if (currentState == CharacterState.Idle)
        {
            HandleIdle();
        }
        else if (currentState == CharacterState.Move)
        {
            HandleMovement();
        }
    }

    protected override void HandleIdle()
    {
        base.HandleIdle();
        animator.Play("Idle");
        LookAtPlayer();
    }

    protected override void HandleMovement()
    {
        MoveTowardsPlayer();
        animator.Play("Move");
    }

    private void MoveTowardsPlayer()
    {
        Vector3 directionToPlayer = (player.position - transform.position).normalized;

        // directionToPlayer가 Vector3.zero인지 확인하고, 아니라면 이동 및 회전 처리
        if (directionToPlayer != Vector3.zero)
        {
            Vector3 targetPosition = transform.position + directionToPlayer * characterData.moveSpeed * Time.deltaTime;

            // 단순 이동 처리
            transform.position = targetPosition;

            // 플레이어를 바라보도록 회전
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }

    private void LookAtPlayer()
    {
        Vector3 directionToPlayer = (player.position - transform.position).normalized;

        // directionToPlayer가 Vector3.zero인지 확인하고, 아니라면 회전 처리
        if (directionToPlayer != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }
}
