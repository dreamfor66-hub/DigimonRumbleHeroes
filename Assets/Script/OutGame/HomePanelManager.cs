using UnityEngine;
using UnityEngine.UI;

public class HomePanelManager : MonoBehaviour
{
    public Button questButton;
    public Button specialMissionButton;
    public Button bossBattleButton;

    public GameObject defaultPanel;
    public GameObject questDungeonListPanel;
    public GameObject specialMissionDungeonListPanel;
    public GameObject bossBattleDungeonListPanel;

    private OutGameUIManager uiManager;

    private void Start()
    {
        uiManager = FindObjectOfType<OutGameUIManager>();

        // 버튼 클릭 이벤트 설정
        questButton.onClick.AddListener(ShowQuestDungeonList);
        specialMissionButton.onClick.AddListener(ShowSpecialMissionDungeonList);
        bossBattleButton.onClick.AddListener(ShowBossBattleDungeonList);
    }

    public void ShowQuestDungeonList()
    {
        CloseAllDungeonLists();
        questDungeonListPanel.SetActive(true);
        defaultPanel.SetActive(false);
    }

    public void ShowSpecialMissionDungeonList()
    {
        CloseAllDungeonLists();
        specialMissionDungeonListPanel.SetActive(true);
        defaultPanel.SetActive(false);
    }

    public void ShowBossBattleDungeonList()
    {
        CloseAllDungeonLists();
        bossBattleDungeonListPanel.SetActive(true);
        defaultPanel.SetActive(false);
    }

    public void ReturnToDefault()
    {
        CloseAllDungeonLists();
        defaultPanel.SetActive(true);
    }

    public bool IsShowingDungeonList()
    {
        // Checks if any of the dungeon list panels are active
        return questDungeonListPanel.activeSelf || specialMissionDungeonListPanel.activeSelf || bossBattleDungeonListPanel.activeSelf;
    }

    private void CloseAllDungeonLists()
    {
        questDungeonListPanel.SetActive(false);
        specialMissionDungeonListPanel.SetActive(false);
        bossBattleDungeonListPanel.SetActive(false);
    }
}
