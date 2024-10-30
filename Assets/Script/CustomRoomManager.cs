using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CustomRoomManager : NetworkRoomManager
{
    // 방 코드가 생성될 때 발생하는 이벤트
    public static event Action OnRoomCodeGenerated;

    // 서버에서 관리할 방 코드 리스트
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

    // 방 코드로 다른 방 찾기
    public bool FindByRoomCode(string code)
    {
        return activeRoomCodes.Contains(code);
    }

    // 클라이언트가 방 코드 확인 요청
    //[Command]
    public void CmdCheckRoomCode(string code)
    {
        bool exists = FindByRoomCode(code);
        RpcReturnCheckRoomCode(exists);
    }

    // 클라이언트에게 방 코드 존재 여부 응답
    //[ClientRpc]
    private void RpcReturnCheckRoomCode(bool exists)
    {
        if (exists)
        {
            Debug.Log("방 코드가 존재합니다. 입장 가능합니다.");
            // 클라이언트 로직: 방에 입장하기
        }
        else
        {
            Debug.LogError("잘못된 방 코드입니다.");
        }
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        activeRoomCodes.Clear(); // 서버가 중지될 때 방 코드 초기화
    }
}
