using UnityEngine;
using Mirror;

public class CustomRoomManager : NetworkRoomManager
{
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);

        // �� ĳ���Ϳ� ���� ������ �Ҵ�
        CharacterBehaviour[] characterBehaviours = FindObjectsOfType<CharacterBehaviour>();

        foreach (var character in characterBehaviours)
        {
            NetworkIdentity netId = character.GetComponent<NetworkIdentity>();
            if (netId != null && netId.connectionToClient == null)
            {
                // �������� Ŭ���̾�Ʈ���� �Ҵ�
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
