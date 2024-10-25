using UnityEngine;
using UnityEngine.UI;

public class OutGameUIManager : MonoBehaviour
{
    public static OutGameUIManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    // Button references
    public Button backButton;
    public Button homeButton;
    public Button characterButton;
    public Button gachaButton;
    public Button menuButton;

    // Panels for different contents
    public GameObject homePanel;
    public GameObject characterPanel;
    public GameObject gachaPanel;
    public GameObject menuPanel;


    // PopUp Panel
    public GameObject popUpPanel; // ÆË¾÷ ÆÐ³Î

    // Panel managers
    [HideInInspector] public HomePanelManager homePanelManager;
    [HideInInspector] public CharacterPanelManager characterPanelManager;
    [HideInInspector] public GachaPanelManager gachaPanelManager;

    private GameObject currentActivePanel;

    // Start is called before the first frame update
    void Start()
    {
        // Set up panel managers by finding their respective components
        homePanelManager = homePanel.GetComponent<HomePanelManager>();
        characterPanelManager = characterPanel.GetComponent<CharacterPanelManager>();
        gachaPanelManager = gachaPanel.GetComponent<GachaPanelManager>();

        // Set up button click listeners
        backButton.onClick.AddListener(OnBackButtonClicked);
        homeButton.onClick.AddListener(OnHomeButtonClicked);
        characterButton.onClick.AddListener(OnCharacterButtonClicked);
        gachaButton.onClick.AddListener(OnGachaButtonClicked);
        menuButton.onClick.AddListener(OnMenuButtonClicked);

        // Show home panel by default
        ShowPanel(homePanel);
        popUpPanel.SetActive(true);
    }

    void ShowPanel(GameObject panel)
    {
        // Disable all panels first
        homePanel.SetActive(false);
        characterPanel.SetActive(false);
        gachaPanel.SetActive(false);
        menuPanel.SetActive(false);

        // Enable the requested panel
        panel.SetActive(true);
        currentActivePanel = panel;

        // Reset home panel to its default state when shown
        if (panel == homePanel && homePanelManager != null)
        {
            homePanelManager.ReturnToDefault();
        }

        // Reset character panel to its default state when shown
        if (panel == characterPanel && characterPanelManager != null)
        {
            characterPanelManager.ReturnToDefault();
        }
    }

    void OnBackButtonClicked()
    {
        // Logic for back button, undo functionality
        if (currentActivePanel == homePanel && homePanelManager.IsShowingDungeonList())
        {
            homePanelManager.ReturnToDefault();
        }
        else if (currentActivePanel == characterPanel)
        {
            if (characterPanelManager != null)
            {
                characterPanelManager.ReturnToDefault();
            }
        }
        else
        {
            ShowPanel(homePanel);
        }
    }

    void OnHomeButtonClicked()
    {
        ShowPanel(homePanel);
    }

    void OnCharacterButtonClicked()
    {
        if (currentActivePanel == characterPanel)
        {
            characterPanelManager.ReturnToDefault();
        }
        ShowPanel(characterPanel);
    }

    void OnGachaButtonClicked()
    {
        ShowPanel(gachaPanel);
    }

    void OnMenuButtonClicked()
    {
        ShowPanel(menuPanel);
    }
}
