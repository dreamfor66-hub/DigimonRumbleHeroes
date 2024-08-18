using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EntityContainer : SingletonBehaviour<EntityContainer>
{
    public List<PlayerController> PlayerCharacters = new List<PlayerController>();
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
        player.playerNumber = PlayerCharacters.Count + 1;  // ���� �ѹ� �ο�
        PlayerCharacters.Add(player);

        if (player.isLeader)
        {
            LeaderPlayer = player;
        }
    }

    public PlayerController GetNextLeader(PlayerController currentLeader)
    {
        // ���� ������ �ѹ��� �������� ���� �÷��̾ ã�´�
        var nextLeader = PlayerCharacters
            .Where(p => p != currentLeader && p != null)
            .OrderBy(p => (p.playerNumber > currentLeader.playerNumber) ? p.playerNumber : int.MaxValue)
            .FirstOrDefault();

        // ���� ���� ������ ���ٸ� ù ��° �÷��̾�� ���ư���
        if (nextLeader == null)
        {
            nextLeader = PlayerCharacters
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

    public void ChangeLeader(PlayerController newLeader)
    {
        foreach (var player in PlayerCharacters)
        {
            player.isLeader = false;
        }

        newLeader.isLeader = true;
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
            PlayerCharacters.Remove(player);
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