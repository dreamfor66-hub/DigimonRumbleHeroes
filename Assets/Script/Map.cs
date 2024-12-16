using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Map : MonoBehaviour
{
    public Vector2 PlayerStartPoint; // X는 맵 그리드 상의 X좌표, Y는 Z좌표를 의미
    public Vector2 MapSize; // 맵의 크기 정보 (MapSize.x = width, MapSize.y = height)

    public void SetPlayerStartPoint(Vector2 newStartPoint)
    {
        PlayerStartPoint = newStartPoint;
        Debug.Log($"Player Start Point set to {PlayerStartPoint}");
    }

}
