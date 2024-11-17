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

    protected override void Start()
    {
        base.Start();
        teamType = TeamType.Enemy;
        ChangeStatePrev(CharacterState.Init);
        FindClosestPlayer();
        UpdateCurrentAIState();
    }

    protected override void Update()
    {
        if (isDie)
            return;
        if (target == null)
            FindClosestPlayer();
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
        ChangeStatePrev(CharacterState.Init);
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


        float distanceToAnyPlayer = float.MaxValue;
        CharacterBehaviour awarePlayer = null;

        foreach (var player in EntityContainer.Instance.PlayerList)
        {
            distanceToAnyPlayer = Vector3.Distance(transform.position, player.transform.position);
            awarePlayer = player;
        }

        if (!isAware && distanceToAnyPlayer <= characterData.attackRange)
        {
            target = awarePlayer;
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
