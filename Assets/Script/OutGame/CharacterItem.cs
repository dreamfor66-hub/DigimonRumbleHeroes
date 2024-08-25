using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterItem : MonoBehaviour
{
    public Image characterImage;
    public TextMeshProUGUI levelText;

    private CharacterItemInfo itemInfo;

    // CharacterItemInfo로부터 UI를 셋업
    public void SetUp(CharacterItemInfo info)
    {
        itemInfo = info;

        // UI 요소에 데이터를 반영
        if (itemInfo.characterData.characterSprite != null)
        {
            characterImage.sprite = itemInfo.characterData.characterSprite;  // 캐릭터 이미지를 데이터에서 가져옴
        }
        else
        {
            // "null_icon"을 포함하는 스프라이트를 Resources 폴더에서 찾아 설정
            Sprite fallbackSprite = Resources.Load<Sprite>("T_CharacterItem_null_icon");
            characterImage.sprite = fallbackSprite;
        }

        levelText.text = "Lv. " + itemInfo.level.ToString();  // 레벨 텍스트를 설정
    }

}