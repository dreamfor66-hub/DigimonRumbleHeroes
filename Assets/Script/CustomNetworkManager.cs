using UnityEngine;
using Mirror;

public class CustomNetworkManager : NetworkManager
{
    public void StartHosting()
    {
        StartHost();
    }

    public void StartJoining()
    {
        StartClient();
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        // �÷��̾ ������ �߰�
        GameObject player = Instantiate(playerPrefab);
        NetworkServer.AddPlayerForConnection(conn, player);
    }

    private void OnEnable()
    {
        // �̺�Ʈ ����
        NetworkClient.OnConnectedEvent += HandleClientConnected;
        NetworkClient.OnDisconnectedEvent += HandleClientDisconnected;
    }

    private void OnDisable()
    {
        // �̺�Ʈ ���� ����
        NetworkClient.OnConnectedEvent -= HandleClientConnected;
        NetworkClient.OnDisconnectedEvent -= HandleClientDisconnected;
    }

    private void HandleClientConnected()
    {
        // ���� ���� �� UI ������Ʈ �� �ʿ��� �۾� ����
        Debug.Log("Ŭ���̾�Ʈ�� ������ ����Ǿ����ϴ�.");
    }

    private void HandleClientDisconnected()
    {
        // ���� ���� �� UI ������Ʈ �� �ʿ��� �۾� ����
        Debug.Log("Ŭ���̾�Ʈ�� �������� ���� �����Ǿ����ϴ�.");
    }
}
