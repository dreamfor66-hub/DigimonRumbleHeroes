using System;
using System.Collections.Generic;
using Mirror;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.UI;

public class UIInputMessageToPlayer : MonoBehaviour
{
    [Serializable]
    public class ButtonMessageDictionary : SerializableDictionary<Button, InputMessage> { }

    // �ν����Ϳ��� ������ �� �ִ� SerializableDictionary
    [OdinSerialize]
    public ButtonMessageDictionary buttonMessageMap;

    private void Start()
    {
        foreach (var kvp in buttonMessageMap)
        {
            var button = kvp.Key;
            var message = kvp.Value;

            if (button != null)
            {
                button.onClick.AddListener(() => SendInputToPlayer(message));
            }
            else
            {
                Debug.LogWarning("Button�� null�Դϴ�.");
            }
        }
    }

    private void SendInputToPlayer(InputMessage message)
    {
        var localPlayer = NetworkClient.localPlayer?.GetComponent<PlayerController>();

        if (localPlayer != null)
        {
            localPlayer.HandleInputMessage(message);
        }
        else
        {
            Debug.LogWarning("LocalPlayer�� �������� �ʽ��ϴ�.");
        }

    }

    // Ư�� ��ư�� Ȱ��ȭ �Ǵ� ��Ȱ��ȭ�ϴ� �޼���
    public void SetButtonActive(Button button, bool isActive)
    {
        if (buttonMessageMap.ContainsKey(button))
        {
            button.gameObject.SetActive(isActive);
        }
        else
        {
            Debug.LogWarning("Button�� buttonMessageMap�� �������� �ʽ��ϴ�.");
        }
    }
}
