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

    // 인스펙터에서 편집할 수 있는 SerializableDictionary
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
                Debug.LogWarning("Button이 null입니다.");
            }
        }
    }

    private void SendInputToPlayer(InputMessage message)
    {
        var localPlayer = NetworkClient.localPlayer?.GetComponent<PlayerController>();

        if (localPlayer != null)
        {
            localPlayer.ReceiveInputMessage(message);
        }
        else
        {
            Debug.LogWarning("LocalPlayer가 존재하지 않습니다.");
        }

    }

    // 특정 버튼을 활성화 또는 비활성화하는 메서드
    public void SetButtonActive(Button button, bool isActive)
    {
        if (buttonMessageMap.ContainsKey(button))
        {
            button.gameObject.SetActive(isActive);
        }
        else
        {
            Debug.LogWarning("Button이 buttonMessageMap에 존재하지 않습니다.");
        }
    }
}
