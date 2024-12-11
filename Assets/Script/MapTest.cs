using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;

public class MapTest : Map
{
    public bool isServer = true; // true: Host�� ����, false: Client�� ����



    void Start()
    {
        InitializeFakeNetwork();
        SpawnPlayer(); // �÷��̾� ���� ȣ��
    }

    void InitializeFakeNetwork()
    {
        if (isServer)
        {
            Debug.Log("Starting as Host...");
            // Mirror���� Host ��� Ȱ��ȭ
            NetworkManager.singleton.StartHost();
        }
        else
        {
            Debug.Log("Starting as Client...");
            NetworkManager.singleton.StartClient();
        }
    }

    void SpawnPlayer()
    {
        if (isServer)
        {
            Debug.Log("Spawning Player for Test Scene...");

            // Player �ν��Ͻ� ���� �� �ʱ�ȭ
            var spawnPosition = new Vector3(PlayerStartPoint.x, 0, PlayerStartPoint.y);
            var playerInstance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);

            // ��Ʈ��ũ ����
            var connection = NetworkServer.localConnection; // ���� Connection ��������
            NetworkServer.AddPlayerForConnection(connection, playerInstance.gameObject);
        }
    }
}
