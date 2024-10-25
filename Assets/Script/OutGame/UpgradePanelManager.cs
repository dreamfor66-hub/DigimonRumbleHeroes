using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class UpgradePanelManager : MonoBehaviour
{
    public static UpgradePanelManager Instance { get; private set; }

    [Header("Panels")]
    public GameObject levelUpPanel;
    public GameObject evolutionPanel;

    [Header("LevelPanel UI Elements")]
    public TextMeshProUGUI expStockText;
    public TextMeshProUGUI leftCharacterNameText;
    public TextMeshProUGUI leftCharacterLevelText;
    public Slider expSlider;
    public TextMeshProUGUI expLeftText;
    public TextMeshProUGUI leftCharacterHPText;
    public TextMeshProUGUI leftCharacterATKText;

    public TextMeshProUGUI rightCharacterNameText;
    public TextMeshProUGUI rightCharacterLevelText;
    public TextMeshProUGUI rightCharacterHPText;
    public TextMeshProUGUI rightCharacterATKText;

    public GameObject character3DModel;
    public Camera renderCamera;
    public Button changeCharacterButton;
    public CharacterModelManager characterModelManager;

    public Button levelUpButton;
    public Button goToEvolutionPanelButton;

    [Header("Evolution Panel UI Elements")]
    public TextMeshProUGUI leftCharacterNameTextEvolution;
    public TextMeshProUGUI leftCharacterLevelTextEvolution;
    public Slider leftExpSliderEvolution;
    public TextMeshProUGUI leftCharacterHPTextEvolution;
    public TextMeshProUGUI leftCharacterATKTextEvolution;

    public TextMeshProUGUI rightCharacterNameTextEvolution;
    public TextMeshProUGUI rightCharacterLevelTextEvolution;
    public Slider rightExpSliderEvolution;
    public TextMeshProUGUI rightCharacterHPTextEvolution;
    public TextMeshProUGUI rightCharacterATKTextEvolution;

    public Button evolutionButton;
    public Button goToLevelUpPanelButton;

    [Header("Character Images")]
    public Image characterImage;
    public Image characterImageNext;
    public Image characterImageSelected;

    [Header("PopUp Elements")]
    public GameObject evolutionConfirmPopUp;
    public Button confirmEvolutionButton;
    public GameObject evolutionSelectPopUp;

    [HideInInspector]
    public CharacterItemInfo selectedCharacter;
    [HideInInspector]
    public CharacterData selectedEvolutionCharacter;
    [HideInInspector]
    public EvolutionData selectedEvolutionData; // 현재 선택된 진화 데이터를 저장
    private Coroutine levelUpCoroutine;
    public InventoryManager inventory;

    private bool isButtonPressed = false;
    private float buttonHoldTime = 0f;

    private const float maxMultiplierTime = 2f;
    private const int maxMultiplier = 3;

    private bool isLevelUp = true;
    private bool isEvolution = false;

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

    private void Start()
    {
        EventTrigger trigger = levelUpButton.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        pointerDownEntry.callback.AddListener((eventData) => OnLevelUpButtonDown());
        trigger.triggers.Add(pointerDownEntry);

        EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        pointerUpEntry.callback.AddListener((eventData) => OnLevelUpButtonUp());
        trigger.triggers.Add(pointerUpEntry);

        changeCharacterButton.onClick.AddListener(OnChangeCharacterButtonClicked);

        goToEvolutionPanelButton.onClick.AddListener(OnGoToEvolutionButtonClicked);
        goToLevelUpPanelButton.onClick.AddListener(ShowLevelUpPanel);

        evolutionButton.onClick.AddListener(OnEvolutionButtonClicked);
        confirmEvolutionButton.onClick.AddListener(OnConfirmEvolutionButtonClicked);

        ShowLevelUpPanel();
    }

    private void OnEnable()
    {
        InitializePanel();
        initialExpStock = ResourceHolder.Instance.playerOutgameData.expStock;
        UpdateExpStockDisplay();

        ShowLevelUpPanel();
    }

    private void InitializePanel()
    {
        var currentParty = ResourceHolder.Instance.playerPartySetUpData.partySetups[ResourceHolder.Instance.playerPartySetUpData.currentPartyIndex];
        selectedCharacter = ResourceHolder.Instance.playerInventoryData.GetCharacterBySerialNumber(currentParty.characterSerialNumbers[0]);

        List<EvolutionData> possibleEvolutions = ResourceHolder.Instance.evolutionTable.GetPossibleEvolutions(selectedCharacter.characterData);
        goToEvolutionPanelButton.gameObject.SetActive(possibleEvolutions.Count > 0);

        UpdateExpStockDisplay();

        if (isLevelUp)
        {
            UpdateCharacterDisplay();
        }
        else if (isEvolution)
        {
            UpdateEvolutionDisplay();
        }
    }

    private int initialExpStock;
    private void UpdateExpStockDisplay()
    {
        expStockText.text = $"{ResourceHolder.Instance.playerOutgameData.expStock} / {initialExpStock}";
    }

    private void UpdateCharacterDisplay()
    {
        leftCharacterNameText.text = selectedCharacter.characterData.characterName;
        leftCharacterLevelText.text = $"{selectedCharacter.level} / {selectedCharacter.maxLevel}";
        leftCharacterHPText.text = $"{CalculateStat(selectedCharacter.level, selectedCharacter.characterData.baseHP, selectedCharacter.characterData.hpPerLevel)}";
        leftCharacterATKText.text = $"{CalculateStat(selectedCharacter.level, selectedCharacter.characterData.baseATK, selectedCharacter.characterData.atkPerLevel)}";

        int nextLevel = selectedCharacter.level + 1;
        rightCharacterNameText.text = selectedCharacter.characterData.characterName;
        rightCharacterLevelText.text = $"<color=green>{nextLevel}</color> / {selectedCharacter.maxLevel}";
        rightCharacterHPText.text = $"{CalculateStat(nextLevel, selectedCharacter.characterData.baseHP, selectedCharacter.characterData.hpPerLevel)}";
        rightCharacterATKText.text = $"{CalculateStat(nextLevel, selectedCharacter.characterData.baseATK, selectedCharacter.characterData.atkPerLevel)}";

        UpdateStatColor(leftCharacterHPText, rightCharacterHPText);
        UpdateStatColor(leftCharacterATKText, rightCharacterATKText);

        UpdateExpUI();

        var mapping = ResourceHolder.Instance.characterVisualMap.GetModelMapping(selectedCharacter.characterData);
        if (mapping != null)
        {
            SetCharacterImage(characterImage, mapping.characterSpriteFull, mapping.spriteMainOffset.pos, mapping.spriteMainOffset.scale);
        }
    }

    public void OnGoToEvolutionButtonClicked()
    {
        List<EvolutionData> possibleEvolutions = ResourceHolder.Instance.evolutionTable.GetPossibleEvolutions(selectedCharacter.characterData);

        if (possibleEvolutions.Count == 1)
        {
            selectedEvolutionCharacter = possibleEvolutions[0].nextCharacterData;
            selectedEvolutionData = possibleEvolutions[0];
            SetEvolutionCharacterImages(possibleEvolutions[0]);
            ShowEvolutionPanel();
        }
        else if (possibleEvolutions.Count > 1)
        {
            PopUpManager.Instance.OpenPopUp(evolutionSelectPopUp);
            PopUpManager.Instance.InitializeEvolutionSelectPopUp(selectedCharacter, possibleEvolutions);
        }
    }

    public void SetSelectedEvolutionCharacter(EvolutionData evolutionData)
    {
        selectedEvolutionData = evolutionData; // 선택한 EvolutionData를 저장
        selectedEvolutionCharacter = evolutionData.nextCharacterData; // nextCharacterData로 설정
        isEvolution = true; // Evolution 모드 활성화
    }

    public void SetEvolutionCharacterImages(EvolutionData evolutionData)
    {
        if (evolutionData != null)
        {
            SetCharacterImage(characterImageSelected, evolutionData.ImageSimulator.ImageSelectedCharacterSprite, evolutionData.ImageSimulator.ImageSelectedCharacterOffset, evolutionData.ImageSimulator.ImageSelectedCharacterScale);
            SetCharacterImage(characterImageNext, evolutionData.ImageSimulator.ImageEvolutionCharacterSprite, evolutionData.ImageSimulator.ImageEvolutionCharacterOffset,evolutionData.ImageSimulator.ImageEvolutionCharacterScale);
        }
    }

    private void SetCharacterImage(Image targetImage, Sprite characterSprite, Vector2 offset, float scale)
    {
        if (targetImage != null && characterSprite != null)
        {
            targetImage.sprite = characterSprite;

            RectTransform rectTransform = targetImage.rectTransform;
            rectTransform.anchoredPosition = offset;
            rectTransform.localScale = Vector3.one * scale;
        }
        else
        {
            Debug.LogError("SetCharacterImage: targetImage or characterSprite is null");
        }
    }

    private void UpdateEvolutionDisplay()
    {
        // 진화 왼쪽 패널 (현재 캐릭터)
        leftCharacterNameTextEvolution.text = selectedCharacter.characterData.characterName;
        leftCharacterLevelTextEvolution.text = $"{selectedCharacter.level} / {selectedCharacter.maxLevel}";
        leftExpSliderEvolution.value = (float)selectedCharacter.expStockConsumed / GetRequiredExpForNextLevel(selectedCharacter.level);
        leftCharacterHPTextEvolution.text = $"{CalculateStat(selectedCharacter.level, selectedCharacter.characterData.baseHP, selectedCharacter.characterData.hpPerLevel)}";
        leftCharacterATKTextEvolution.text = $"{CalculateStat(selectedCharacter.level, selectedCharacter.characterData.baseATK, selectedCharacter.characterData.atkPerLevel)}";

        // 진화 오른쪽 패널 (진화 후 캐릭터)
        rightCharacterNameTextEvolution.text = selectedEvolutionCharacter.characterName;
        rightCharacterLevelTextEvolution.text = $"{selectedCharacter.level} / {selectedCharacter.maxLevel}";
        rightExpSliderEvolution.value = leftExpSliderEvolution.value;
        rightCharacterHPTextEvolution.text = $"{CalculateStat(selectedCharacter.level, selectedEvolutionCharacter.baseHP, selectedEvolutionCharacter.hpPerLevel)}";
        rightCharacterATKTextEvolution.text = $"{CalculateStat(selectedCharacter.level, selectedEvolutionCharacter.baseATK, selectedEvolutionCharacter.atkPerLevel)}";

        // 이미지 설정
        SetCharacterImage(characterImageSelected, selectedEvolutionData.ImageSimulator.ImageSelectedCharacterSprite, selectedEvolutionData.ImageSimulator.ImageSelectedCharacterOffset, selectedEvolutionData.ImageSimulator.ImageSelectedCharacterScale);
        SetCharacterImage(characterImageNext, selectedEvolutionData.ImageSimulator.ImageEvolutionCharacterSprite, selectedEvolutionData.ImageSimulator.ImageEvolutionCharacterOffset, selectedEvolutionData.ImageSimulator.ImageEvolutionCharacterScale);
    }

    private float CalculateStat(int level, float baseStat, float statPerLevel)
    {
        return baseStat + (statPerLevel * (level - 1));
    }

    private void UpdateStatColor(TextMeshProUGUI leftText, TextMeshProUGUI rightText)
    {
        if (leftText.text != rightText.text)
        {
            rightText.color = Color.green;
        }
        else
        {
            rightText.color = Color.black;
        }
    }

    public void OnLevelUpButtonDown()
    {
        isButtonPressed = true;
        levelUpCoroutine = StartCoroutine(LevelUpRoutine());
    }

    public void OnLevelUpButtonUp()
    {
        isButtonPressed = false;
        if (levelUpCoroutine != null)
        {
            StopCoroutine(levelUpCoroutine);
            levelUpCoroutine = null;
        }
        buttonHoldTime = 0f;
    }

    private IEnumerator LevelUpRoutine()
    {
        float currentInterval = 0.05f;
        float minInterval = 0.01f;
        float intervalDecreaseRate = 0.1f;

        while (true)
        {
            if (ResourceHolder.Instance.playerOutgameData.expStock > 0)
            {
                selectedCharacter.expStockConsumed++;
                ResourceHolder.Instance.playerOutgameData.expStock--;

                UpdateExpStockDisplay();
                CheckForLevelUp();
                UpdateExpUI();

                currentInterval = Mathf.Max(minInterval, currentInterval - (intervalDecreaseRate * Time.deltaTime));
            }

            yield return new WaitForSeconds(currentInterval);
        }
    }

    private void CheckForLevelUp()
    {
        int requiredExpForNextLevel = GetRequiredExpForNextLevel(selectedCharacter.level);
        if (selectedCharacter.expStockConsumed >= requiredExpForNextLevel && selectedCharacter.level < selectedCharacter.maxLevel)
        {
            selectedCharacter.level++;
            selectedCharacter.expStockConsumed = 0;
            UpdateCharacterDisplay();
        }
    }

    private int GetRequiredExpForNextLevel(int currentLevel)
    {
        return ResourceHolder.Instance.gameVariables.expStockRequiredPerLevel[currentLevel - 1];
    }

    private void UpdateExpUI()
    {
        int requiredExp = GetRequiredExpForNextLevel(selectedCharacter.level);
        int remainingExp = requiredExp - selectedCharacter.expStockConsumed;

        expSlider.value = (float)selectedCharacter.expStockConsumed / requiredExp;
        expLeftText.text = $"{remainingExp} left";
    }

    public void OnCharacterModelClick()
    {
        inventory.OpenInventoryForCharacterSelection();
    }

    public void SetSelectedCharacter(CharacterItemInfo selectedCharacter)
    {
        this.selectedCharacter = selectedCharacter;
        UpdateCharacterDisplay();

        List<EvolutionData> possibleEvolutions = ResourceHolder.Instance.evolutionTable.GetPossibleEvolutions(selectedCharacter.characterData);
        goToEvolutionPanelButton.gameObject.SetActive(possibleEvolutions.Count > 0);
    }

    private void OnChangeCharacterButtonClicked()
    {
        inventory.OpenInventoryForCharacterSelection();
    }

    public void ShowLevelUpPanel()
    {
        isLevelUp = true;
        isEvolution = false;
        levelUpPanel.SetActive(true);
        evolutionPanel.SetActive(false);
    }

    public void ShowEvolutionPanel()
    {
        isLevelUp = false;
        isEvolution = true;
        levelUpPanel.SetActive(false);
        evolutionPanel.SetActive(true);

        UpdateEvolutionDisplay();
    }

    public void OnEvolutionButtonClicked()
    {
        if (selectedCharacter.level >= selectedCharacter.maxLevel)
        {
            evolutionConfirmPopUp.SetActive(true);
        }
        else
        {
            PopUpManager.Instance.OpenPopUp(PopUpManager.Instance.EvolutionErrorPopUp);
        }
    }

    public void OnConfirmEvolutionButtonClicked()
    {
        selectedCharacter.characterData = selectedEvolutionCharacter;

        UpdateCharacterDisplay();
        ShowLevelUpPanel();

        evolutionConfirmPopUp.SetActive(false);
    }
}
