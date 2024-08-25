using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public PlayerInventoryData playerInventoryData;
    public GameObject characterItemPrefab;
    public Transform inventoryGrid;

    // PartySetUp ���� ����
    public PartySetUpManager partySetUpManager;
    public LevelUpPanelManager levelUpPanelManager;

    // InventoryPanel ���� ����
    public GameObject levelUpInventoryPanel;

    // ���õ� ������ �����ϱ� ���� ����
    private int selectedSlotIndex = -1;

    private void Start()
    {
        InitializeInventory();
    }
    private void OnEnable()
    {
        InitializeInventory();
    }

    // �κ��丮�� �ʱ�ȭ�ϰ�, ���� �������� ĳ���� �������� ǥ��
    public void InitializeInventory()
    {
        foreach (Transform child in inventoryGrid)
        {
            Destroy(child.gameObject);
        }

        foreach (var characterItemInfo in playerInventoryData.characterItems)
        {
            CreateCharacterItem(characterItemInfo);
        }
    }

    // CharacterItemInfo�� ������� CharacterItem �������� �ν��Ͻ�ȭ�Ͽ� �κ��丮�� �߰�
    private void CreateCharacterItem(CharacterItemInfo itemInfo)
    {
        GameObject newItem = Instantiate(characterItemPrefab, inventoryGrid);
        CharacterItem characterItemComponent = newItem.GetComponent<CharacterItem>();

        // CharacterItem�� ǥ���� �����͸� ����
        characterItemComponent.SetUp(itemInfo);

        // ��ư Ŭ�� �� �ش� ĳ���͸� �����ϴ� �̺�Ʈ ���
        Button itemButton = newItem.GetComponent<Button>();
        itemButton.onClick.AddListener(() => OnCharacterItemClicked(itemInfo));
    }



    public void OnCharacterItemClicked(CharacterItemInfo selectedItem)
    {
        if (partySetUpManager != null && partySetUpManager.partySetUpInventoryPanel.activeSelf)
        {
            // ���õ� �������� �ø��� �ѹ��� ����Ͽ� ��Ƽ ���Կ� ��ġ
            partySetUpManager.SetCharacterInSlot(selectedSlotIndex, selectedItem.serialNumber);

            // �κ��丮 �г��� ����
            partySetUpManager.CloseInventoryPanel();
        }
        else if (levelUpPanelManager != null && levelUpInventoryPanel.activeSelf)
        {
            // LevelUpPanelManager���� ȣ��� ���
            levelUpPanelManager.SetSelectedCharacter(selectedItem);

            // �κ��丮 �г��� ����
            levelUpInventoryPanel.SetActive(false);
        }
    }



    // PartySetUp���� ȣ��Ǿ�, ���õ� ������ ����
    //public void OpenInventoryForSlot(int slotIndex)
    //{
    //    selectedSlotIndex = slotIndex;
    //    inventoryPanel.SetActive(true);
    //}
    public void OpenInventoryForSlot(int slotIndex)
    {
        selectedSlotIndex = slotIndex;
        partySetUpManager.partySetUpInventoryPanel.SetActive(true);
        InitializeInventory(); // �κ��丮 �г��� ���鼭 ĳ���� ����� �ʱ�ȭ
    }

    public void OpenInventoryForCharacterSelection()
    {
        this.gameObject.SetActive(true);
        InitializeInventory(); // �κ��丮 �г��� ���鼭 ĳ���� ����� �ʱ�ȭ
    }

    public void OpenInventoryForSlot(int slotIndex, bool forLevelUp = false)
    {
        selectedSlotIndex = slotIndex;
        if (forLevelUp)
        {
            levelUpInventoryPanel.SetActive(true);
        }
        else
        {
            partySetUpManager.partySetUpInventoryPanel.SetActive(true);
        }

        InitializeInventory(); // �κ��丮 �г��� ���鼭 ĳ���� ����� �ʱ�ȭ
    }
}
