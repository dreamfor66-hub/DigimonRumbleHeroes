using UnityEngine;
using Mirror;
using TMPro; // TextMeshPro 사용 시 필요

public class OnlineSceneManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField roomCodeInput; // 방 코드 입력 필드

    public NetworkRoomManager roomManager;

    private void Start()
    {
        // NetworkRoomManager 인스턴스 가져오기
        roomManager = FindObjectOfType<NetworkRoomManager>();
    }

    public void CreateRoom()
    {
        if (roomManager == null)
        {
            Debug.LogError("NetworkRoomManager를 찾을 수 없습니다.");
            return;
        }

        // 서버가 이미 실행 중인지 확인
        if (roomManager.isNetworkActive)
        {
            Debug.LogWarning("이미 서버가 실행 중입니다.");

            // 서버가 실행 중인 상태라면 RoomScene으로 이동할지 여부 결정
            if (roomManager.mode == NetworkManagerMode.Host)
            {
                Debug.Log("호스트 모드입니다. RoomScene으로 이동 중...");
                if (roomManager.RoomScene != null && roomManager.RoomScene != "")
                {
                    roomManager.ServerChangeScene(roomManager.RoomScene); // RoomScene으로 이동
                }
                else
                {
                    Debug.LogError("RoomScene이 설정되어 있지 않습니다.");
                }
            }
            return;
        }

        // 서버가 실행 중이 아닐 때만 새로운 방 생성
        roomManager.StartHost();
        Debug.Log("방 생성 및 호스트 시작");
    }
    public void JoinRoom()
    {
        if (roomManager != null && !roomManager.isNetworkActive)
        {
            roomManager.networkAddress = "localhost"; // 같은 PC의 서버로 접속
            roomManager.StartClient(); // 클라이언트로 접속 시작
            Debug.Log("클라이언트로 접속 시도 중...");
        }
        else
        {
            Debug.LogWarning("이미 클라이언트가 실행 중입니다.");
        }
    }
}
