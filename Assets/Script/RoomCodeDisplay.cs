using UnityEngine;
using TMPro;
using Mirror;

public class RoomCodeDisplay : MonoBehaviour
{
    public TextMeshProUGUI roomCodeText;
    public TMP_InputField roomCodeInputField; // 입력 필드 추가
    private CustomRoomManager roomManager;

    private void Start()
    {
        roomManager = NetworkManager.singleton as CustomRoomManager;
    }

    private void OnEnable()
    {
        CustomRoomManager.OnRoomCodeGenerated += UpdateRoomCodeDisplay;
    }

    private void OnDisable()
    {
        CustomRoomManager.OnRoomCodeGenerated -= UpdateRoomCodeDisplay;
    }

    private void UpdateRoomCodeDisplay()
    {
        if (roomManager != null && !string.IsNullOrEmpty(roomManager.roomCode))
        {
            roomCodeText.text = "Room Code: " + roomManager.roomCode;
            Debug.Log("Room Code Displayed: " + roomManager.roomCode);
        }
        else
        {
            Debug.LogError("Room Code를 찾을 수 없습니다.");
        }
    }

    // JoinRoom 메서드
    public void JoinRoom()
    {
        string enteredCode = roomCodeInputField.text;

        if (roomManager != null)
        {
            // 현재 방 코드와 입력한 코드가 동일한지 확인
            if (roomManager.roomCode == enteredCode)
            {
                Debug.LogError("현재 방 번호입니다."); // 에러 메시지 출력
                return; // 방 입장 로직을 진행하지 않음
            }

            // 입력한 코드가 서버에 존재하는지 확인
            roomManager.CmdCheckRoomCode(enteredCode); // 서버에 확인 요청
        }
    }
}
