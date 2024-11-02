using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class CustomRoomManager : NetworkRoomManager
{
    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        if (SceneManager.GetActiveScene().name != RoomScene)
        {
            Debug.Log("클라이언트가 Room 씬이 아닌 곳에 있지만 연결을 유지합니다.");
            return;
        }
        base.OnServerConnect(conn);
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Debug.Log("OnServerAddPlayer 호출됨.");
        base.OnServerAddPlayer(conn);
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("클라이언트가 서버에 연결되었습니다.");

        // 씬 전환 시 자동 준비
        if (SceneManager.GetActiveScene().name == RoomScene)
        {
            if (!NetworkClient.ready)
            {
                NetworkClient.Ready();
                Debug.Log("클라이언트가 Room 씬에서 준비 상태로 설정되었습니다.");
            }
        }

        // 씬 로드 이벤트 등록
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == RoomScene)
        {
            Debug.Log("클라이언트가 Room 씬에 진입했습니다.");
            if (!NetworkClient.ready)
            {
                NetworkClient.Ready();
                Debug.Log("클라이언트가 Room 씬에서 준비 상태로 설정되었습니다.");
            }
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
