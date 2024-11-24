using UnityEngine;
using Mirror;

public class CustomRoomManager : NetworkRoomManager
{
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);

        // 적 캐릭터에 대한 소유권 할당
        CharacterBehaviour[] characterBehaviours = FindObjectsOfType<CharacterBehaviour>();

        foreach (var character in characterBehaviours)
        {
            NetworkIdentity netId = character.GetComponent<NetworkIdentity>();
            if (netId != null && netId.connectionToClient == null)
            {
                // 소유권을 클라이언트에게 할당
                netId.AssignClientAuthority(conn);
            }
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("Client started.");
    }

    public void OnClientConnectInternal(NetworkConnection conn)
    {
        Debug.Log("Client connected.");
        if (!NetworkClient.ready)
        {
            NetworkClient.Ready();
        }
    }
}
