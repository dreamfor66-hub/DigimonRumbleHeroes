using Mirror;
using UnityEngine;

public class AutoConnect : MonoBehaviour
{
    public bool startAsHost = true; // Host�� �������� Ŭ���̾�Ʈ�� �������� �����ϴ� �÷���

    private void Start()
    {
        NetworkRoomManager roomManager = GetComponent<NetworkRoomManager>();
        if (roomManager != null && !roomManager.isNetworkActive)
        {
            if (startAsHost)
            {
                // Host�� ����
                roomManager.StartHost();
                Debug.Log("����(Host)�� �ڵ����� ���۵Ǿ����ϴ�.");
            }
            else
            {
                // Client�� ����
                roomManager.networkAddress = "localhost"; // ���� �ּ� ����
                roomManager.StartClient();
                Debug.Log("Ŭ���̾�Ʈ(Client)�� ���� �õ� ��...");
            }
        }
    }
}
