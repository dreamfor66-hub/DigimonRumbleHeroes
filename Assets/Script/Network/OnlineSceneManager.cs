using UnityEngine;
using Mirror;
using TMPro; // TextMeshPro ��� �� �ʿ�

public class OnlineSceneManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField roomCodeInput; // �� �ڵ� �Է� �ʵ�

    public NetworkRoomManager roomManager;

    private void Start()
    {
        // NetworkRoomManager �ν��Ͻ� ��������
        roomManager = FindObjectOfType<NetworkRoomManager>();
    }

    public void CreateRoom()
    {
        if (roomManager == null)
        {
            Debug.LogError("NetworkRoomManager�� ã�� �� �����ϴ�.");
            return;
        }

        // ������ �̹� ���� ������ Ȯ��
        if (roomManager.isNetworkActive)
        {
            Debug.LogWarning("�̹� ������ ���� ���Դϴ�.");

            // ������ ���� ���� ���¶�� RoomScene���� �̵����� ���� ����
            if (roomManager.mode == NetworkManagerMode.Host)
            {
                Debug.Log("ȣ��Ʈ ����Դϴ�. RoomScene���� �̵� ��...");
                if (roomManager.RoomScene != null && roomManager.RoomScene != "")
                {
                    roomManager.ServerChangeScene(roomManager.RoomScene); // RoomScene���� �̵�
                }
                else
                {
                    Debug.LogError("RoomScene�� �����Ǿ� ���� �ʽ��ϴ�.");
                }
            }
            return;
        }

        // ������ ���� ���� �ƴ� ���� ���ο� �� ����
        roomManager.StartHost();
        Debug.Log("�� ���� �� ȣ��Ʈ ����");
    }
    public void JoinRoom()
    {
        if (roomManager != null && !roomManager.isNetworkActive)
        {
            roomManager.networkAddress = "localhost"; // ���� PC�� ������ ����
            roomManager.StartClient(); // Ŭ���̾�Ʈ�� ���� ����
            Debug.Log("Ŭ���̾�Ʈ�� ���� �õ� ��...");
        }
        else
        {
            Debug.LogWarning("�̹� Ŭ���̾�Ʈ�� ���� ���Դϴ�.");
        }
    }
}
