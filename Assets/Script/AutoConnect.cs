using Mirror;
using UnityEngine;

public class AutoConnect : MonoBehaviour
{
    public bool startAsHost = true; // Host로 시작할지 클라이언트로 시작할지 결정하는 플래그

    private void Start()
    {
        NetworkRoomManager roomManager = GetComponent<NetworkRoomManager>();
        if (roomManager != null && !roomManager.isNetworkActive)
        {
            if (startAsHost)
            {
                // Host로 시작
                roomManager.StartHost();
                Debug.Log("서버(Host)가 자동으로 시작되었습니다.");
            }
            else
            {
                // Client로 시작
                roomManager.networkAddress = "localhost"; // 서버 주소 설정
                roomManager.StartClient();
                Debug.Log("클라이언트(Client)로 접속 시도 중...");
            }
        }
    }
}
