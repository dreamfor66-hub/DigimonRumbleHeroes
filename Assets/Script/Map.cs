using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Map : MonoBehaviour
{
    public Vector2 PlayerStartPoint; // X�� �� �׸��� ���� X��ǥ, Y�� Z��ǥ�� �ǹ�
    public Vector2 MapSize; // ���� ũ�� ���� (MapSize.x = width, MapSize.y = height)

    public void SetPlayerStartPoint(Vector2 newStartPoint)
    {
        PlayerStartPoint = newStartPoint;
        Debug.Log($"Player Start Point set to {PlayerStartPoint}");
    }

}
