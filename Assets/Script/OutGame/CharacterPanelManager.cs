using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterPanelManager : MonoBehaviour
{
    public Button partySetupButton;
    public Button levelUpButton;
    public Button trainingButton;
    public Button characterListButton;

    public GameObject defaultPanel;  // DefaultPanel 추가
    public GameObject partySetupPanel;
    public GameObject levelUpPanel;
    public GameObject trainingPanel;
    public GameObject characterListPanel;
    public GameObject partySetUpInventoryPanel;
    public GameObject LevelUpInventoryPanel;

    // Start is called before the first frame update
    void Start()
    {
        // DefaultPanel의 버튼들에 리스너 연결
        partySetupButton.onClick.AddListener(OnPartySetupButtonClicked);
        levelUpButton.onClick.AddListener(OnLevelUpButtonClicked);
        trainingButton.onClick.AddListener(OnTrainingButtonClicked);
        characterListButton.onClick.AddListener(OnCharacterListButtonClicked);

        // 초기화 상태에서 DefaultPanel 활성화
        ReturnToDefault();
    }

    void OnPartySetupButtonClicked()
    {
        partySetupPanel.SetActive(true);
        levelUpPanel.SetActive(false);
        trainingPanel.SetActive(false);
        characterListPanel.SetActive(false);
        defaultPanel.SetActive(false);  // DefaultPanel 비활성화
    }

    void OnLevelUpButtonClicked()
    {
        partySetupPanel.SetActive(false);
        levelUpPanel.SetActive(true);
        trainingPanel.SetActive(false);
        characterListPanel.SetActive(false);
        defaultPanel.SetActive(false);  // DefaultPanel 비활성화
    }

    void OnTrainingButtonClicked()
    {
        partySetupPanel.SetActive(false);
        levelUpPanel.SetActive(false);
        trainingPanel.SetActive(true);
        characterListPanel.SetActive(false);
        defaultPanel.SetActive(false);  // DefaultPanel 비활성화
    }

    void OnCharacterListButtonClicked()
    {
        partySetupPanel.SetActive(false);
        levelUpPanel.SetActive(false);
        trainingPanel.SetActive(false);
        characterListPanel.SetActive(true);
        defaultPanel.SetActive(false);  // DefaultPanel 비활성화
    }

    // DefaultPanel로 돌아오는 메서드
    public void ReturnToDefault()
    {
        defaultPanel.SetActive(true);
        partySetupPanel.SetActive(false);
        levelUpPanel.SetActive(false);
        trainingPanel.SetActive(false);
        characterListPanel.SetActive(false);
        partySetUpInventoryPanel.SetActive(false);
        LevelUpInventoryPanel.SetActive(false);
    }
}
