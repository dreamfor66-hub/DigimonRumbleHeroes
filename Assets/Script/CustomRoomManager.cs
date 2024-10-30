using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CustomRoomManager : NetworkRoomManager
{
    // �� �ڵ尡 ������ �� �߻��ϴ� �̺�Ʈ
    public static event Action OnRoomCodeGenerated;

    // �������� ������ �� �ڵ� ����Ʈ
    private HashSet<string> activeRoomCodes = new HashSet<string>();

    public string roomCode;

    public override void OnStartServer()
    {
        base.OnStartServer();
        GenerateUniqueRoomCode();
    }

    private void GenerateUniqueRoomCode()
    {
        string newCode;
        do
        {
            newCode = GenerateRoomCode();
        } while (activeRoomCodes.Contains(newCode));

        roomCode = newCode;
        activeRoomCodes.Add(roomCode);

        Debug.Log("Room Code Generated: " + roomCode);
        OnRoomCodeGenerated?.Invoke();
    }

    private string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        char[] stringChars = new char[6];
        System.Random random = new System.Random();

        for (int i = 0; i < stringChars.Length; i++)
        {
            stringChars[i] = chars[random.Next(chars.Length)];
        }

        return new string(stringChars);
    }

    public override void OnRoomStopServer()
    {
        if (!string.IsNullOrEmpty(roomCode))
        {
            activeRoomCodes.Remove(roomCode);
            Debug.Log("Room Code Removed: " + roomCode);
        }
        base.OnRoomStopServer();
    }

    // �� �ڵ�� �ٸ� �� ã��
    public bool FindByRoomCode(string code)
    {
        return activeRoomCodes.Contains(code);
    }

    // Ŭ���̾�Ʈ�� �� �ڵ� Ȯ�� ��û
    //[Command]
    public void CmdCheckRoomCode(string code)
    {
        bool exists = FindByRoomCode(code);
        RpcReturnCheckRoomCode(exists);
    }

    // Ŭ���̾�Ʈ���� �� �ڵ� ���� ���� ����
    //[ClientRpc]
    private void RpcReturnCheckRoomCode(bool exists)
    {
        if (exists)
        {
            Debug.Log("�� �ڵ尡 �����մϴ�. ���� �����մϴ�.");
            // Ŭ���̾�Ʈ ����: �濡 �����ϱ�
        }
        else
        {
            Debug.LogError("�߸��� �� �ڵ��Դϴ�.");
        }
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        activeRoomCodes.Clear(); // ������ ������ �� �� �ڵ� �ʱ�ȭ
    }
}
