using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterPanelManager : MonoBehaviour
{
    public Button partySetupButton;
    public Button levelUpButton;
    public Button trainingButton;
    public Button characterListButton;

    public GameObject defaultPanel;  // DefaultPanel �߰�
    public GameObject partySetupPanel;
    public GameObject levelUpPanel;
    public GameObject trainingPanel;
    public GameObject characterListPanel;
    public GameObject partySetUpInventoryPanel;
    public GameObject LevelUpInventoryPanel;

    // Start is called before the first frame update
    void Start()
    {
        // DefaultPanel�� ��ư�鿡 ������ ����
        partySetupButton.onClick.AddListener(OnPartySetupButtonClicked);
        levelUpButton.onClick.AddListener(OnLevelUpButtonClicked);
        trainingButton.onClick.AddListener(OnTrainingButtonClicked);
        characterListButton.onClick.AddListener(OnCharacterListButtonClicked);

        // �ʱ�ȭ ���¿��� DefaultPanel Ȱ��ȭ
        ReturnToDefault();
    }

    void OnPartySetupButtonClicked()
    {
        partySetupPanel.SetActive(true);
        levelUpPanel.SetActive(false);
        trainingPanel.SetActive(false);
        characterListPanel.SetActive(false);
        defaultPanel.SetActive(false);  // DefaultPanel ��Ȱ��ȭ
    }

    void OnLevelUpButtonClicked()
    {
        partySetupPanel.SetActive(false);
        levelUpPanel.SetActive(true);
        trainingPanel.SetActive(false);
        characterListPanel.SetActive(false);
        defaultPanel.SetActive(false);  // DefaultPanel ��Ȱ��ȭ
    }

    void OnTrainingButtonClicked()
    {
        partySetupPanel.SetActive(false);
        levelUpPanel.SetActive(false);
        trainingPanel.SetActive(true);
        characterListPanel.SetActive(false);
        defaultPanel.SetActive(false);  // DefaultPanel ��Ȱ��ȭ
    }

    void OnCharacterListButtonClicked()
    {
        partySetupPanel.SetActive(false);
        levelUpPanel.SetActive(false);
        trainingPanel.SetActive(false);
        characterListPanel.SetActive(true);
        defaultPanel.SetActive(false);  // DefaultPanel ��Ȱ��ȭ
    }

    // DefaultPanel�� ���ƿ��� �޼���
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
