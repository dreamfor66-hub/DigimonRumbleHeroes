using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;

public class LevelUpPanelManager : MonoBehaviour
{
    public static LevelUpPanelManager Instance { get; private set; }

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

    public Button levelUpButton;
    public GameObject character3DModel;
    public Camera renderCamera;

    private CharacterItemInfo selectedCharacter;
    private Coroutine levelUpCoroutine;

    public InventoryManager inventory;

    private bool isButtonPressed = false;
    private float buttonHoldTime = 0f;

    private const float maxMultiplierTime = 2f; // �ִ� ������ �Ǵ� �ð� (2��)
    private const int maxMultiplier = 3; // �ִ� ����

    public Button changeCharacterButton;  // ĳ���� ��ü ��ư

    public CharacterModelManager characterModelManager;



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
        // EventTrigger�� ����Ͽ� ��ư ������ �������� ����
        EventTrigger trigger = levelUpButton.gameObject.AddComponent<EventTrigger>();

        // PointerDown �̺�Ʈ ���� (��ư�� ������ ��)
        EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        pointerDownEntry.callback.AddListener((eventData) => OnLevelUpButtonDown());
        trigger.triggers.Add(pointerDownEntry);

        // PointerUp �̺�Ʈ ���� (��ư�� �������� ��)
        EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        pointerUpEntry.callback.AddListener((eventData) => OnLevelUpButtonUp());
        trigger.triggers.Add(pointerUpEntry);

        changeCharacterButton.onClick.AddListener(OnChangeCharacterButtonClicked);
    }

    private void OnEnable()
    {
        InitializePanel();
        initialExpStock = ResourceHolder.Instance.playerOutgameData.expStock;
        UpdateExpStockDisplay();
    }

    private void InitializePanel()
    {
        // ���� ��Ƽ�� ������ �����ͼ� selectedCharacter�� ����
        var currentParty = ResourceHolder.Instance.playerPartySetUpData.partySetups[ResourceHolder.Instance.playerPartySetUpData.currentPartyIndex];
        selectedCharacter = ResourceHolder.Instance.playerInventoryData.GetCharacterBySerialNumber(currentParty.characterSerialNumbers[0]); // 0�� ������ ������ ����

        // 3D �� �ʱ�ȭ
        characterModelManager.InitializeCharacter3DModel(selectedCharacter);


        UpdateExpStockDisplay();
        UpdateCharacterDisplay();
    }

    private int initialExpStock;
    private void UpdateExpStockDisplay()
    {
        // ������ �ǽð����� ���ŵǴ� ��, ������ ������ �ʱ� ��
        expStockText.text = $"{ResourceHolder.Instance.playerOutgameData.expStock} / {initialExpStock}";
    }

    private void UpdateCharacterDisplay()
    {
        // ���� �г�
        leftCharacterNameText.text = selectedCharacter.characterData.characterName;
        leftCharacterLevelText.text = $"{selectedCharacter.level} / {selectedCharacter.maxLevel}";
        leftCharacterHPText.text = $"{CalculateStat(selectedCharacter.level, selectedCharacter.characterData.baseHP, selectedCharacter.characterData.hpPerLevel)}";
        leftCharacterATKText.text = $"{CalculateStat(selectedCharacter.level, selectedCharacter.characterData.baseATK, selectedCharacter.characterData.atkPerLevel)}";

        // ������ �г� (���� ����)
        int nextLevel = selectedCharacter.level + 1;
        rightCharacterNameText.text = selectedCharacter.characterData.characterName;
        rightCharacterLevelText.text = $"<color=green>{nextLevel}</color> / {selectedCharacter.maxLevel}";
        rightCharacterHPText.text = $"{CalculateStat(nextLevel, selectedCharacter.characterData.baseHP, selectedCharacter.characterData.hpPerLevel)}";
        rightCharacterATKText.text = $"{CalculateStat(nextLevel, selectedCharacter.characterData.baseATK, selectedCharacter.characterData.atkPerLevel)}";

        // ���� ���̿� ���� �ؽ�Ʈ ���� ����
        UpdateStatColor(leftCharacterHPText, rightCharacterHPText);
        UpdateStatColor(leftCharacterATKText, rightCharacterATKText);

        UpdateExpUI();
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
        buttonHoldTime = 0f; // ��ư ���� ���� �ð� �ʱ�ȭ
    }

    private IEnumerator LevelUpRoutine()
    {
        float currentInterval = 0.05f; // �ʱ� ���� �ֱ�
        float minInterval = 0.01f; // �ּ� ���� �ֱ�
        float intervalDecreaseRate = 0.1f; // �ֱ� ���� �ӵ�

        while (true)
        {
            if (ResourceHolder.Instance.playerOutgameData.expStock > 0)
            {
                selectedCharacter.expStockConsumed++;
                ResourceHolder.Instance.playerOutgameData.expStock--;

                UpdateExpStockDisplay();
                CheckForLevelUp();
                UpdateExpUI();

                // ���� �ֱ� ����
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

        // ����ġ �����̴��� "xx left" �ؽ�Ʈ ������Ʈ
        expSlider.value = (float)selectedCharacter.expStockConsumed / requiredExp;
        expLeftText.text = $"{remainingExp} left";
    }


    public void OnCharacterModelClick()
    {
        // ĳ���� ��ü �г� ȣ��
        inventory.OpenInventoryForCharacterSelection();
    }

    public void SetSelectedCharacter(CharacterItemInfo selectedCharacter)
    {
        this.selectedCharacter = selectedCharacter;
        UpdateCharacterDisplay();
    }

    private void OnChangeCharacterButtonClicked()
    {
        // LevelUpInventoryPanel�� ���ϴ�.
        inventory.OpenInventoryForCharacterSelection();
    }

    //public void InitializeCharacter3DModel(CharacterItemInfo character)
    //{
    //    // ���� ĳ���� 3D ���� �ִٸ� ����
    //    if (character3DModel != null)
    //    {
    //        Destroy(character3DModel);
    //    }

    //    // ModelTable���� �ش� ĳ���� �����Ϳ� ���ε� �𵨰� �ִϸ����͸� ������
    //    var modelMapping = ResourceHolder.Instance.modelTable.GetModelMapping(character.characterData);
    //    if (modelMapping == null)
    //    {
    //        Debug.LogError($"ModelMapping not found for {character.characterData.name}");
    //        return;
    //    }

    //    // �� ���ӿ�����Ʈ ����
    //    GameObject characterModelRoot = new GameObject("CharacterModelRoot");
    //    characterModelRoot.transform.position = renderCamera.transform.position;
    //    characterModelRoot.transform.rotation = Quaternion.identity;

    //    // Animator ������Ʈ �߰� �� ����
    //    Animator animator = characterModelRoot.AddComponent<Animator>();
    //    animator.runtimeAnimatorController = modelMapping.animator;

    //    // �� �������� ���� ������Ʈ�� �ν��Ͻ�ȭ
    //    character3DModel = Instantiate(modelMapping.characterPrefab, Vector3.zero, Quaternion.identity, characterModelRoot.transform);

    //    // ĳ���� ���� ��ġ�� ȸ���� �ʱ�ȭ (�ʿ信 ���� ���� ����)
    //    character3DModel.transform.localPosition = Vector3.zero;
    //    character3DModel.transform.localRotation = Quaternion.identity;

    //    // ĳ���� ���� ī�޶��� �ڽ����� ����
    //    characterModelRoot.transform.SetParent(renderCamera.transform);
    //}
}
