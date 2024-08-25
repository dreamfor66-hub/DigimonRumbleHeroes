using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public PlayerInventoryData playerInventoryData;
    public GameObject characterItemPrefab;
    public Transform inventoryGrid;

    // PartySetUp 관련 참조
    public PartySetUpManager partySetUpManager;
    public LevelUpPanelManager levelUpPanelManager;

    // InventoryPanel 관련 참조
    public GameObject levelUpInventoryPanel;

    // 선택된 슬롯을 추적하기 위한 변수
    private int selectedSlotIndex = -1;

    private void Start()
    {
        InitializeInventory();
    }
    private void OnEnable()
    {
        InitializeInventory();
    }

    // 인벤토리를 초기화하고, 현재 소유중인 캐릭터 아이템을 표시
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

    // CharacterItemInfo를 기반으로 CharacterItem 프리팹을 인스턴스화하여 인벤토리에 추가
    private void CreateCharacterItem(CharacterItemInfo itemInfo)
    {
        GameObject newItem = Instantiate(characterItemPrefab, inventoryGrid);
        CharacterItem characterItemComponent = newItem.GetComponent<CharacterItem>();

        // CharacterItem이 표시할 데이터를 설정
        characterItemComponent.SetUp(itemInfo);

        // 버튼 클릭 시 해당 캐릭터를 선택하는 이벤트 등록
        Button itemButton = newItem.GetComponent<Button>();
        itemButton.onClick.AddListener(() => OnCharacterItemClicked(itemInfo));
    }



    public void OnCharacterItemClicked(CharacterItemInfo selectedItem)
    {
        if (partySetUpManager != null && partySetUpManager.partySetUpInventoryPanel.activeSelf)
        {
            // 선택된 아이템의 시리얼 넘버를 사용하여 파티 슬롯에 배치
            partySetUpManager.SetCharacterInSlot(selectedSlotIndex, selectedItem.serialNumber);

            // 인벤토리 패널을 닫음
            partySetUpManager.CloseInventoryPanel();
        }
        else if (levelUpPanelManager != null && levelUpInventoryPanel.activeSelf)
        {
            // LevelUpPanelManager에서 호출된 경우
            levelUpPanelManager.SetSelectedCharacter(selectedItem);

            // 인벤토리 패널을 닫음
            levelUpInventoryPanel.SetActive(false);
        }
    }



    // PartySetUp에서 호출되어, 선택된 슬롯을 설정
    //public void OpenInventoryForSlot(int slotIndex)
    //{
    //    selectedSlotIndex = slotIndex;
    //    inventoryPanel.SetActive(true);
    //}
    public void OpenInventoryForSlot(int slotIndex)
    {
        selectedSlotIndex = slotIndex;
        partySetUpManager.partySetUpInventoryPanel.SetActive(true);
        InitializeInventory(); // 인벤토리 패널을 열면서 캐릭터 목록을 초기화
    }

    public void OpenInventoryForCharacterSelection()
    {
        this.gameObject.SetActive(true);
        InitializeInventory(); // 인벤토리 패널을 열면서 캐릭터 목록을 초기화
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

        InitializeInventory(); // 인벤토리 패널을 열면서 캐릭터 목록을 초기화
    }
}
