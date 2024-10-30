using UnityEngine;
using TMPro;
using Mirror;

public class RoomCodeDisplay : MonoBehaviour
{
    public TextMeshProUGUI roomCodeText;
    public TMP_InputField roomCodeInputField; // �Է� �ʵ� �߰�
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
            Debug.LogError("Room Code�� ã�� �� �����ϴ�.");
        }
    }

    // JoinRoom �޼���
    public void JoinRoom()
    {
        string enteredCode = roomCodeInputField.text;

        if (roomManager != null)
        {
            // ���� �� �ڵ�� �Է��� �ڵ尡 �������� Ȯ��
            if (roomManager.roomCode == enteredCode)
            {
                Debug.LogError("���� �� ��ȣ�Դϴ�."); // ���� �޽��� ���
                return; // �� ���� ������ �������� ����
            }

            // �Է��� �ڵ尡 ������ �����ϴ��� Ȯ��
            roomManager.CmdCheckRoomCode(enteredCode); // ������ Ȯ�� ��û
        }
    }
}
