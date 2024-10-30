using Mirror;
using UnityEngine;

public class AutoConnect : MonoBehaviour
{
    void Start()
    {
        // 네트워크 서버로 자동 연결 시도
        if (!NetworkClient.isConnected && !NetworkServer.active)
        {
            NetworkManager.singleton.StartHost();  // 자동으로 방을 생성하며 호스트로 연결
        }
    }

    void OnConnectedToServer()
    {
        // 연결이 완료되면 자동으로 Online 씬으로 이동
        NetworkManager.singleton.ServerChangeScene("OnlineScene");
    }
}
