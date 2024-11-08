using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Mirror;

public class EntityContainer : SingletonBehaviour<EntityContainer>
{
    public List<PlayerController> PlayerList = new List<PlayerController>();
    //public PlayerController LeaderPlayer;

    private CameraController cameraController;
    public List<CharacterBehaviour> CharacterList { get; private set; } = new List<CharacterBehaviour>();

    public override void Init()
    {
        // �ʱ�ȭ �ڵ尡 �ʿ��ϸ� ���⿡ �ۼ�
        cameraController = FindObjectOfType<CameraController>();
    }

    public void RegisterPlayer(PlayerController player)
    {
        player.playerNumber = PlayerList.Count + 1;  // ���� �ѹ� �ο�
        PlayerList.Add(player);

        
    }


    public void RegisterCharacter(CharacterBehaviour character)
    {
        if (!CharacterList.Contains(character))
        {
            CharacterList.Add(character);

            // ���� �ο� ���� �߰�
            if (NetworkServer.active && character.TryGetComponent(out NetworkIdentity identity))
            {
                // �� �κ��� �������� ����Ǹ�, Ŭ���̾�Ʈ ���� ��ü�� ������ ���� �ο�
                NetworkConnectionToClient conn = identity.connectionToClient as NetworkConnectionToClient;

                if (conn != null && !identity.isOwned)
                {
                    identity.AssignClientAuthority(conn);
                    Debug.Log($"Authority assigned to {character.name} for connection ID {conn.connectionId}");
                }
            }
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