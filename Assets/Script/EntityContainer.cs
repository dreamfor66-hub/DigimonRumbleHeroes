using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EntityContainer : SingletonBehaviour<EntityContainer>
{
    public List<PlayerController> PlayerList = new List<PlayerController>();
    public PlayerController LeaderPlayer;

    private CameraController cameraController;
    public List<CharacterBehaviour> CharacterList { get; private set; } = new List<CharacterBehaviour>();

    public override void Init()
    {
        // �ʱ�ȭ �ڵ尡 �ʿ��ϸ� ���⿡ �ۼ�
        cameraController = FindObjectOfType<CameraController>();
        if (cameraController != null && LeaderPlayer != null)
        {
            cameraController.UpdateTarget(LeaderPlayer.transform);
        }
    }

    public void RegisterPlayer(PlayerController player)
    {
        player.playerNumber = PlayerList.Count + 1;  // ���� �ѹ� �ο�
        PlayerList.Add(player);

        if (player.isLeader)
        {
            LeaderPlayer = player;
        }
    }

    public PlayerController GetNextLeader(PlayerController currentLeader)
    {
        // ���� ������ �ѹ��� �������� ���� �÷��̾ ã�´�
        var nextLeader = PlayerList
            .Where(p => p != currentLeader && p != null)
            .OrderBy(p => (p.playerNumber > currentLeader.playerNumber) ? p.playerNumber : int.MaxValue)
            .FirstOrDefault();

        // ���� ���� ������ ���ٸ� ù ��° �÷��̾�� ���ư���
        if (nextLeader == null)
        {
            nextLeader = PlayerList
                .Where(p => p != currentLeader && p != null)
                .OrderBy(p => p.playerNumber)
                .FirstOrDefault();
        }

        return nextLeader;
    }

    public void RegisterCharacter(CharacterBehaviour character)
    {
        if (!CharacterList.Contains(character))
        {
            CharacterList.Add(character);
        }
    }

    public void ChangeLeader(PlayerController currentLeader, PlayerController newLeader)
    {
        foreach (var player in PlayerList)
        {
            player.isLeader = false;
        }

        if (currentLeader.currentState == CharacterState.Move)
            currentLeader.currentState = CharacterState.Idle;
        currentLeader.currentAIState = AIState.Follow;

        newLeader.isLeader = true;
        newLeader.lastAttacker = null;
        newLeader.lastHitTarget = null;
        if (newLeader.currentState == CharacterState.Move)
            newLeader.currentState = CharacterState.Idle;
        LeaderPlayer = newLeader;

        // ������ �ٲ� �� ī�޶� Ÿ�� ������Ʈ
        if (cameraController != null)
        {
            cameraController.UpdateTarget(newLeader.transform);
        }
    }

    public void UnregisterCharacter(CharacterBehaviour character)
    {
        if (character is PlayerController player)
        {
            PlayerList.Remove(player);
        }

        if (CharacterList.Contains(character))
        {
            CharacterList.Remove(character);
        }
    }

    public CharacterBehaviour GetCharacterNearestTo(Vector3 position)
    {
        return CharacterList.OrderBy(c => Vector3.Distance(c.transform.position, position)).FirstOrDefault();
    }
}