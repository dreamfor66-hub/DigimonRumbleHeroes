using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EntityContainer : SingletonBehaviour<EntityContainer>
{
    public List<PlayerController> PlayerCharacters = new List<PlayerController>();
    public PlayerController LeaderPlayer;
    public List<CharacterBehaviour> CharacterList { get; private set; } = new List<CharacterBehaviour>();

    public override void Init()
    {
        // �ʱ�ȭ �ڵ尡 �ʿ��ϸ� ���⿡ �ۼ�
    }

    public void RegisterPlayer(PlayerController player)
    {
        PlayerCharacters.Add(player);
        if (player.isLeader)
        {
            LeaderPlayer = player;
        }
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

        // ī�޶� ���� �� ���� �ڵ� �߰�
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