using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopUpManager : MonoBehaviour
{
    public static PopUpManager Instance { get; private set; }

    public GameObject EvolutionConfirmPopUp;
    public GameObject EvolutionSelectPopUp;
    public GameObject EvolutionErrorPopUp;

    public Image selectedCharacterIcon;
    public Transform evolutionOptionsContainer;
    public GameObject evolutionOptionPrefab;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Start()
    {
        EvolutionConfirmPopUp.SetActive(false);
        EvolutionSelectPopUp.SetActive(false);
        EvolutionErrorPopUp.SetActive(false);
    }

    public void InitializeEvolutionSelectPopUp(CharacterItemInfo selectedCharacter, List<EvolutionData> possibleEvolutions)
    {
        // 현재 캐릭터 아이콘 설정
        selectedCharacterIcon.sprite = selectedCharacter.characterData.characterSprite; // 아이콘 사용

        // 기존에 생성된 아이콘들을 제거
        foreach (Transform child in evolutionOptionsContainer)
        {
            Destroy(child.gameObject);
        }

        // 진화 가능한 캐릭터 아이콘 설정
        for (int i = 0; i < possibleEvolutions.Count; i++)
        {
            EvolutionData evolutionData = possibleEvolutions[i];
            GameObject optionObject = Instantiate(evolutionOptionPrefab, evolutionOptionsContainer);

            Image iconImage = optionObject.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = evolutionData.nextCharacterData.characterSprite; // 캐릭터 아이콘 사용
            }

            Button button = optionObject.GetComponent<Button>();
            if (button != null)
            {
                int index = i; // 클로저 이슈 방지
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    UpgradePanelManager.Instance.SetSelectedEvolutionCharacter(evolutionData);
                    PopUpManager.Instance.ClosePopUp(PopUpManager.Instance.EvolutionSelectPopUp);
                    UpgradePanelManager.Instance.ShowEvolutionPanel();
                });
            }
        }
    }

    public void OpenPopUp(GameObject popUp)
    {
        popUp.SetActive(true);
    }

    public void ClosePopUp(GameObject popUp)
    {
        popUp.SetActive(false);
    }
}
