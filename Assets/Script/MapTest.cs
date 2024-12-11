using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;

public class MapTest : Map
{
    public bool isServer = true; // true: Host로 실행, false: Client로 실행



    void Start()
    {
        InitializeFakeNetwork();
        SpawnPlayer(); // 플레이어 생성 호출
    }

    void InitializeFakeNetwork()
    {
        if (isServer)
        {
            Debug.Log("Starting as Host...");
            // Mirror에서 Host 모드 활성화
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

            // Player 인스턴스 생성 및 초기화
            var spawnPosition = new Vector3(PlayerStartPoint.x, 0, PlayerStartPoint.y);
            var playerInstance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);

            // 네트워크 스폰
            var connection = NetworkServer.localConnection; // 로컬 Connection 가져오기
            NetworkServer.AddPlayerForConnection(connection, playerInstance.gameObject);
        }
    }
}
