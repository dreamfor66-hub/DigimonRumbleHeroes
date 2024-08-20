using Sirenix.OdinInspector;
using UnityEngine;

public class EnemyController : CharacterBehaviour
{
    public BotAIData botAIData;
    [ShowInInspector]
    private bool isAware = false;
    [ShowInInspector]
    private float stateTimer = 0f;
    private int currentBotAIIndex = 0;
    private float duration = 0f;

    protected override void Start()
    {
        base.Start();
        currentState = CharacterState.Init;
        FindClosestPlayer();
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
        float distanceToPlayer = Vector3.Distance(transform.position, target.transform.position);

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
        float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);

        if (!isAware && distanceToTarget <= characterData.attackRange)
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
        if (target == null) return;

        Vector3 directionToTarget = (target.transform.position - transform.position).normalized;

        if (directionToTarget != Vector3.zero)
        {
            Vector3 targetPosition = transform.position + directionToTarget * characterData.moveSpeed * Time.deltaTime;

            transform.position = targetPosition;

            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
        animator.Play("Move");
    }

    private void FindClosestPlayer()
    {
        float closestDistance = float.MaxValue;
        PlayerController closestPlayer = null;

        foreach (var character in EntityContainer.Instance.CharacterList)
        {
            if (character is PlayerController player)
            {
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
        Vector3 directionToTarget = (target.transform.position - transform.position).normalized;

        // directionToTarget Vector3.zero인지 확인하고, 아니라면 회전 처리
        if (directionToTarget != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }
}
