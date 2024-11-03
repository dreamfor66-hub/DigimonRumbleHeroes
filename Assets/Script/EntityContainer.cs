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
        // 초기화 코드가 필요하면 여기에 작성
        cameraController = FindObjectOfType<CameraController>();
    }

    public void RegisterPlayer(PlayerController player)
    {
        player.playerNumber = PlayerList.Count + 1;  // 고유 넘버 부여
        PlayerList.Add(player);

        
    }


    public void RegisterCharacter(CharacterBehaviour character)
    {
        if (!CharacterList.Contains(character))
        {
            CharacterList.Add(character);

            // 권한 부여 로직 추가
            if (NetworkServer.active && character.TryGetComponent(out NetworkIdentity identity))
            {
                // 이 부분은 서버에서 실행되며, 클라이언트 연결 객체를 가져와 권한 부여
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