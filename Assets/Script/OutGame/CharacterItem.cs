using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterItem : MonoBehaviour
{
    public Image characterImage;
    public TextMeshProUGUI levelText;

    private CharacterItemInfo itemInfo;

    // CharacterItemInfo�κ��� UI�� �¾�
    public void SetUp(CharacterItemInfo info)
    {
        itemInfo = info;

        // UI ��ҿ� �����͸� �ݿ�
        if (itemInfo.characterData.characterSprite != null)
        {
            characterImage.sprite = itemInfo.characterData.characterSprite;  // ĳ���� �̹����� �����Ϳ��� ������
        }
        else
        {
            // "null_icon"�� �����ϴ� ��������Ʈ�� Resources �������� ã�� ����
            Sprite fallbackSprite = Resources.Load<Sprite>("T_CharacterItem_null_icon");
            characterImage.sprite = fallbackSprite;
        }

        levelText.text = "Lv. " + itemInfo.level.ToString();  // ���� �ؽ�Ʈ�� ����
    }

}