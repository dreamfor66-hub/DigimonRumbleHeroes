using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class CustomRoomManager : NetworkRoomManager
{
    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        if (SceneManager.GetActiveScene().name != RoomScene)
        {
            Debug.Log("Ŭ���̾�Ʈ�� Room ���� �ƴ� ���� ������ ������ �����մϴ�.");
            return;
        }
        base.OnServerConnect(conn);
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Debug.Log("OnServerAddPlayer ȣ���.");
        base.OnServerAddPlayer(conn);
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("Ŭ���̾�Ʈ�� ������ ����Ǿ����ϴ�.");

        // �� ��ȯ �� �ڵ� �غ�
        if (SceneManager.GetActiveScene().name == RoomScene)
        {
            if (!NetworkClient.ready)
            {
                NetworkClient.Ready();
                Debug.Log("Ŭ���̾�Ʈ�� Room ������ �غ� ���·� �����Ǿ����ϴ�.");
            }
        }

        // �� �ε� �̺�Ʈ ���
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == RoomScene)
        {
            Debug.Log("Ŭ���̾�Ʈ�� Room ���� �����߽��ϴ�.");
            if (!NetworkClient.ready)
            {
                NetworkClient.Ready();
                Debug.Log("Ŭ���̾�Ʈ�� Room ������ �غ� ���·� �����Ǿ����ϴ�.");
            }
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
