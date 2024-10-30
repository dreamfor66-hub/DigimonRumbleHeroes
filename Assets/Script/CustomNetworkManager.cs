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
        // 플레이어를 서버에 추가
        GameObject player = Instantiate(playerPrefab);
        NetworkServer.AddPlayerForConnection(conn, player);
    }

    private void OnEnable()
    {
        // 이벤트 구독
        NetworkClient.OnConnectedEvent += HandleClientConnected;
        NetworkClient.OnDisconnectedEvent += HandleClientDisconnected;
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        NetworkClient.OnConnectedEvent -= HandleClientConnected;
        NetworkClient.OnDisconnectedEvent -= HandleClientDisconnected;
    }

    private void HandleClientConnected()
    {
        // 연결 성공 시 UI 업데이트 등 필요한 작업 수행
        Debug.Log("클라이언트가 서버에 연결되었습니다.");
    }

    private void HandleClientDisconnected()
    {
        // 연결 해제 시 UI 업데이트 등 필요한 작업 수행
        Debug.Log("클라이언트가 서버에서 연결 해제되었습니다.");
    }
}
