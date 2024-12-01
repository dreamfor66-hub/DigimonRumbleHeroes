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
            CmdAddStatus(StatusType.SuperArmor, -1f); // -1�� ���� ���۾Ƹ� ���� �߰�
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
                // ��ٿ� ���� Ȯ��
                if (state.nextState == CharacterState.Action && state.IsOnCooldown())
                {
                    continue; // ��ٿ� ���̶�� ���� ���·� �Ѿ
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
    // BotAIState�� ��ٿ� Ÿ�̸Ӹ� ���������� ������Ʈ
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
        float normalizedSpeed = 0; // �׻� 1�� ����
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

        // �浹�� �����̵��� ó���� ������ ���
        Vector3 moveVector = HandleCollisionAndSliding(directionToTarget, currentSpeed);
        transform.position += moveVector;

        // ���̸� 0���� ����
        transform.position = new Vector3(transform.position.x, 0, transform.position.z);

        // ���� ���� �� ȸ��
        if (directionToTarget != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }

        // �ִϸ��̼� ó��
        animator.Play("Idle");
        float normalizedSpeed = 1; // �׻� 1�� ����
        animator.SetFloat("speed", normalizedSpeed);

        //// ��Ʈ��ũ ����ȭ (���� �÷��̾��� ����)
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
                if (player == null) // �÷��̾ null���� Ȯ��
                    continue;

                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlayer = player;
                }
            }
        }

        target = closestPlayer; // ���� ����� �÷��̾ Ÿ������ ����
    }

    private void LookAtPlayer()
    {
        if (target == null)
            return;

        Vector3 directionToTarget = (target.transform.position - transform.position).normalized;

        // directionToTarget Vector3.zero���� Ȯ���ϰ�, �ƴ϶�� ȸ�� ó��
        if (directionToTarget != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }
}
