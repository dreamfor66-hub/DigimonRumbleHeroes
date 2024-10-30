using Mirror;
using UnityEngine;

public class AutoConnect : MonoBehaviour
{
    void Start()
    {
        // ��Ʈ��ũ ������ �ڵ� ���� �õ�
        if (!NetworkClient.isConnected && !NetworkServer.active)
        {
            NetworkManager.singleton.StartHost();  // �ڵ����� ���� �����ϸ� ȣ��Ʈ�� ����
        }
    }

    void OnConnectedToServer()
    {
        // ������ �Ϸ�Ǹ� �ڵ����� Online ������ �̵�
        NetworkManager.singleton.ServerChangeScene("OnlineScene");
    }
}
