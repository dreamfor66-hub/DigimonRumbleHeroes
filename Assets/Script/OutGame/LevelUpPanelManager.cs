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

    private const float maxMultiplierTime = 2f; // 최대 배율이 되는 시간 (2초)
    private const int maxMultiplier = 3; // 최대 배율

    public Button changeCharacterButton;  // 캐릭터 교체 버튼

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
        // EventTrigger를 사용하여 버튼 눌림과 떼어짐을 감지
        EventTrigger trigger = levelUpButton.gameObject.AddComponent<EventTrigger>();

        // PointerDown 이벤트 설정 (버튼이 눌렸을 때)
        EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        pointerDownEntry.callback.AddListener((eventData) => OnLevelUpButtonDown());
        trigger.triggers.Add(pointerDownEntry);

        // PointerUp 이벤트 설정 (버튼이 떼어졌을 때)
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
        // 현재 파티의 리더를 가져와서 selectedCharacter로 설정
        var currentParty = ResourceHolder.Instance.playerPartySetUpData.partySetups[ResourceHolder.Instance.playerPartySetUpData.currentPartyIndex];
        selectedCharacter = ResourceHolder.Instance.playerInventoryData.GetCharacterBySerialNumber(currentParty.characterSerialNumbers[0]); // 0번 슬롯이 리더로 가정

        // 3D 모델 초기화
        characterModelManager.InitializeCharacter3DModel(selectedCharacter);


        UpdateExpStockDisplay();
        UpdateCharacterDisplay();
    }

    private int initialExpStock;
    private void UpdateExpStockDisplay()
    {
        // 앞쪽은 실시간으로 갱신되는 값, 뒤쪽은 고정된 초기 값
        expStockText.text = $"{ResourceHolder.Instance.playerOutgameData.expStock} / {initialExpStock}";
    }

    private void UpdateCharacterDisplay()
    {
        // 왼쪽 패널
        leftCharacterNameText.text = selectedCharacter.characterData.characterName;
        leftCharacterLevelText.text = $"{selectedCharacter.level} / {selectedCharacter.maxLevel}";
        leftCharacterHPText.text = $"{CalculateStat(selectedCharacter.level, selectedCharacter.characterData.baseHP, selectedCharacter.characterData.hpPerLevel)}";
        leftCharacterATKText.text = $"{CalculateStat(selectedCharacter.level, selectedCharacter.characterData.baseATK, selectedCharacter.characterData.atkPerLevel)}";

        // 오른쪽 패널 (다음 레벨)
        int nextLevel = selectedCharacter.level + 1;
        rightCharacterNameText.text = selectedCharacter.characterData.characterName;
        rightCharacterLevelText.text = $"<color=green>{nextLevel}</color> / {selectedCharacter.maxLevel}";
        rightCharacterHPText.text = $"{CalculateStat(nextLevel, selectedCharacter.characterData.baseHP, selectedCharacter.characterData.hpPerLevel)}";
        rightCharacterATKText.text = $"{CalculateStat(nextLevel, selectedCharacter.characterData.baseATK, selectedCharacter.characterData.atkPerLevel)}";

        // 스탯 차이에 따라 텍스트 색상 변경
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
        buttonHoldTime = 0f; // 버튼 떼면 누른 시간 초기화
    }

    private IEnumerator LevelUpRoutine()
    {
        float currentInterval = 0.05f; // 초기 증가 주기
        float minInterval = 0.01f; // 최소 증가 주기
        float intervalDecreaseRate = 0.1f; // 주기 감소 속도

        while (true)
        {
            if (ResourceHolder.Instance.playerOutgameData.expStock > 0)
            {
                selectedCharacter.expStockConsumed++;
                ResourceHolder.Instance.playerOutgameData.expStock--;

                UpdateExpStockDisplay();
                CheckForLevelUp();
                UpdateExpUI();

                // 증가 주기 감소
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

        // 경험치 슬라이더와 "xx left" 텍스트 업데이트
        expSlider.value = (float)selectedCharacter.expStockConsumed / requiredExp;
        expLeftText.text = $"{remainingExp} left";
    }


    public void OnCharacterModelClick()
    {
        // 캐릭터 교체 패널 호출
        inventory.OpenInventoryForCharacterSelection();
    }

    public void SetSelectedCharacter(CharacterItemInfo selectedCharacter)
    {
        this.selectedCharacter = selectedCharacter;
        UpdateCharacterDisplay();
    }

    private void OnChangeCharacterButtonClicked()
    {
        // LevelUpInventoryPanel을 엽니다.
        inventory.OpenInventoryForCharacterSelection();
    }

    //public void InitializeCharacter3DModel(CharacterItemInfo character)
    //{
    //    // 기존 캐릭터 3D 모델이 있다면 삭제
    //    if (character3DModel != null)
    //    {
    //        Destroy(character3DModel);
    //    }

    //    // ModelTable에서 해당 캐릭터 데이터에 매핑된 모델과 애니메이터를 가져옴
    //    var modelMapping = ResourceHolder.Instance.modelTable.GetModelMapping(character.characterData);
    //    if (modelMapping == null)
    //    {
    //        Debug.LogError($"ModelMapping not found for {character.characterData.name}");
    //        return;
    //    }

    //    // 빈 게임오브젝트 생성
    //    GameObject characterModelRoot = new GameObject("CharacterModelRoot");
    //    characterModelRoot.transform.position = renderCamera.transform.position;
    //    characterModelRoot.transform.rotation = Quaternion.identity;

    //    // Animator 컴포넌트 추가 및 설정
    //    Animator animator = characterModelRoot.AddComponent<Animator>();
    //    animator.runtimeAnimatorController = modelMapping.animator;

    //    // 모델 프리팹을 하위 오브젝트로 인스턴스화
    //    character3DModel = Instantiate(modelMapping.characterPrefab, Vector3.zero, Quaternion.identity, characterModelRoot.transform);

    //    // 캐릭터 모델의 위치와 회전을 초기화 (필요에 따라 조정 가능)
    //    character3DModel.transform.localPosition = Vector3.zero;
    //    character3DModel.transform.localRotation = Quaternion.identity;

    //    // 캐릭터 모델을 카메라의 자식으로 설정
    //    characterModelRoot.transform.SetParent(renderCamera.transform);
    //}
}
